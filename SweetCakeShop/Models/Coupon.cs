namespace SweetCakeShop.Models
{
    public enum DiscountType
    {
        /// <summary>Fixed amount off (e.g., 50,000 VND off)</summary>
        FixedAmount = 0,
        /// <summary>Percentage off (e.g., 10% off)</summary>
        Percentage = 1
    }

    public enum CouponScope
    {
        /// <summary>Applies to entire order</summary>
        Global = 0,
        /// <summary>Applies only to products in a specific category</summary>
        Category = 1,
        /// <summary>Applies only to a specific product</summary>
        Product = 2
    }

    /// <summary>
    /// Coupon / promotion code with scoped discounts, time-based validity,
    /// and usage limits (global + per-user via CouponUsage tracking).
    /// </summary>
    public class Coupon
    {
        public int CouponId { get; set; }

        /// <summary>Unique, case-insensitive coupon code (e.g., "SUMMER2026").</summary>
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        /// <summary>Discount value — percentage (0-100) or fixed VND amount.</summary>
        public decimal DiscountValue { get; set; }

        /// <summary>Minimum order subtotal required to use this coupon (0 = no minimum).</summary>
        public decimal MinOrderAmount { get; set; }

        /// <summary>Maximum discount cap for percentage coupons (0 = no cap).</summary>
        public decimal MaxDiscountAmount { get; set; }

        public DateTime ValidFrom { get; set; } = DateTime.Now;
        public DateTime ValidTo { get; set; } = DateTime.Now.AddDays(30);

        /// <summary>Maximum total uses across all users (0 = unlimited).</summary>
        public int MaxUsageCount { get; set; }

        /// <summary>How many times this coupon has been used so far.</summary>
        public int CurrentUsageCount { get; set; }

        /// <summary>Maximum uses per individual user (0 = unlimited).</summary>
        public int MaxUsagePerUser { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        // Scope
        public CouponScope Scope { get; set; } = CouponScope.Global;

        /// <summary>Required when Scope == Category.</summary>
        public int? ScopeCategoryId { get; set; }

        /// <summary>Required when Scope == Product.</summary>
        public int? ScopeProductId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
    }
}
