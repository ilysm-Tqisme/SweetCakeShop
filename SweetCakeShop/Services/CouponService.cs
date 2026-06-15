using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public class CouponValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public Coupon? Coupon { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    /// <summary>
    /// Coupon validation engine with scoped discounts, time-based rules,
    /// and usage limit enforcement (global + per-user).
    /// </summary>
    public interface ICouponService
    {
        Task<CouponValidationResult> ValidateAsync(string? code, decimal orderSubtotal, string userId, List<Models.ViewModels.CartItemViewModel>? items = null);
        Task RecordUsageAsync(int couponId, string userId, int orderId, decimal discountApplied);
        Task<List<Coupon>> GetAllCouponsAsync();
        Task<Coupon?> GetByIdAsync(int couponId);
        Task<Coupon> CreateAsync(Coupon coupon);
        Task<Coupon?> UpdateAsync(Coupon coupon);
        Task<bool> DeleteAsync(int couponId);
        Task<bool> ToggleActiveAsync(int couponId);
    }

    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _context;

        public CouponService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CouponValidationResult> ValidateAsync(
            string? code, decimal orderSubtotal, string userId,
            List<Models.ViewModels.CartItemViewModel>? items = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Fail("Vui lòng nhập mã giảm giá.");
            }

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant());

            if (coupon == null)
            {
                return Fail("Mã giảm giá không tồn tại.");
            }

            if (!coupon.IsActive)
            {
                return Fail("Mã giảm giá đã bị vô hiệu hóa.");
            }

            var now = DateTime.Now;
            if (now < coupon.ValidFrom)
            {
                return Fail($"Mã giảm giá chưa có hiệu lực (bắt đầu từ {coupon.ValidFrom:dd/MM/yyyy}).");
            }

            if (now > coupon.ValidTo)
            {
                return Fail("Mã giảm giá đã hết hạn.");
            }

            // Global usage limit
            if (coupon.MaxUsageCount > 0 && coupon.CurrentUsageCount >= coupon.MaxUsageCount)
            {
                return Fail("Mã giảm giá đã hết lượt sử dụng.");
            }

            // Per-user usage limit
            if (coupon.MaxUsagePerUser > 0 && !string.IsNullOrEmpty(userId))
            {
                var userUsageCount = await _context.CouponUsages
                    .CountAsync(cu => cu.CouponId == coupon.CouponId && cu.UserId == userId);

                if (userUsageCount >= coupon.MaxUsagePerUser)
                {
                    return Fail("Bạn đã sử dụng mã giảm giá này rồi.");
                }
            }

            // Minimum order amount
            if (coupon.MinOrderAmount > 0 && orderSubtotal < coupon.MinOrderAmount)
            {
                return Fail($"Đơn hàng tối thiểu {coupon.MinOrderAmount:N0}₫ để sử dụng mã này.");
            }

            // Calculate applicable subtotal based on scope
            var applicableSubtotal = orderSubtotal;
            if (items != null && items.Count > 0)
            {
                applicableSubtotal = CalculateApplicableSubtotal(coupon, items);
                if (applicableSubtotal <= 0)
                {
                    return Fail("Không có sản phẩm nào trong giỏ hàng áp dụng được mã giảm giá này.");
                }
            }

            // Calculate discount
            decimal discountAmount;
            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discountAmount = applicableSubtotal * (coupon.DiscountValue / 100m);
                // Apply max discount cap
                if (coupon.MaxDiscountAmount > 0 && discountAmount > coupon.MaxDiscountAmount)
                {
                    discountAmount = coupon.MaxDiscountAmount;
                }
            }
            else // FixedAmount
            {
                discountAmount = coupon.DiscountValue;
            }

            // Discount cannot exceed order subtotal
            discountAmount = Math.Min(discountAmount, orderSubtotal);
            discountAmount = Math.Round(discountAmount, 0, MidpointRounding.AwayFromZero);

            return new CouponValidationResult
            {
                IsValid = true,
                Message = $"Áp dụng thành công! Giảm {discountAmount:N0}₫",
                Coupon = coupon,
                DiscountAmount = discountAmount
            };
        }

        private decimal CalculateApplicableSubtotal(Coupon coupon, List<Models.ViewModels.CartItemViewModel> items)
        {
            switch (coupon.Scope)
            {
                case CouponScope.Global:
                    return items.Sum(i => i.Price * i.Quantity);

                case CouponScope.Category:
                    // We would need category info on cart items — for now apply globally
                    return items.Sum(i => i.Price * i.Quantity);

                case CouponScope.Product:
                    if (coupon.ScopeProductId.HasValue)
                    {
                        return items
                            .Where(i => i.ProductId == coupon.ScopeProductId.Value)
                            .Sum(i => i.Price * i.Quantity);
                    }
                    return 0;

                default:
                    return items.Sum(i => i.Price * i.Quantity);
            }
        }

        public async Task RecordUsageAsync(int couponId, string userId, int orderId, decimal discountApplied)
        {
            var coupon = await _context.Coupons.FindAsync(couponId);
            if (coupon == null) return;

            coupon.CurrentUsageCount++;

            _context.CouponUsages.Add(new CouponUsage
            {
                CouponId = couponId,
                UserId = userId,
                OrderId = orderId,
                DiscountApplied = discountApplied,
                UsedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<Coupon>> GetAllCouponsAsync()
        {
            return await _context.Coupons
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Coupon?> GetByIdAsync(int couponId)
        {
            return await _context.Coupons.FindAsync(couponId);
        }

        public async Task<Coupon> CreateAsync(Coupon coupon)
        {
            coupon.Code = coupon.Code.Trim().ToUpperInvariant();
            coupon.CreatedAt = DateTime.Now;
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            return coupon;
        }

        public async Task<Coupon?> UpdateAsync(Coupon coupon)
        {
            var existing = await _context.Coupons.FindAsync(coupon.CouponId);
            if (existing == null) return null;

            existing.Code = coupon.Code.Trim().ToUpperInvariant();
            existing.Description = coupon.Description;
            existing.DiscountType = coupon.DiscountType;
            existing.DiscountValue = coupon.DiscountValue;
            existing.MinOrderAmount = coupon.MinOrderAmount;
            existing.MaxDiscountAmount = coupon.MaxDiscountAmount;
            existing.ValidFrom = coupon.ValidFrom;
            existing.ValidTo = coupon.ValidTo;
            existing.MaxUsageCount = coupon.MaxUsageCount;
            existing.MaxUsagePerUser = coupon.MaxUsagePerUser;
            existing.IsActive = coupon.IsActive;
            existing.Scope = coupon.Scope;
            existing.ScopeCategoryId = coupon.ScopeCategoryId;
            existing.ScopeProductId = coupon.ScopeProductId;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int couponId)
        {
            var coupon = await _context.Coupons.FindAsync(couponId);
            if (coupon == null) return false;

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int couponId)
        {
            var coupon = await _context.Coupons.FindAsync(couponId);
            if (coupon == null) return false;

            coupon.IsActive = !coupon.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        private static CouponValidationResult Fail(string message) => new()
        {
            IsValid = false,
            Message = message
        };
    }
}
