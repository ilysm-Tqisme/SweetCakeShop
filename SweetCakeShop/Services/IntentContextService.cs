using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services
{
    public class IntentContextService : IIntentContextService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevenueService _revenueService;

        public IntentContextService(ApplicationDbContext context, IRevenueService revenueService)
        {
            _context = context;
            _revenueService = revenueService;
        }

        public async Task<string> BuildContextAsync(
            AiChatMode mode,
            IReadOnlyList<ChatIntent> intents,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            return mode == AiChatMode.Admin
                ? await BuildAdminContextAsync(intents, cancellationToken)
                : await BuildCustomerContextAsync(intents, userMessage, cancellationToken);
        }

        private async Task<string> BuildAdminContextAsync(
            IReadOnlyList<ChatIntent> intents,
            CancellationToken cancellationToken)
        {
            var sections = new List<string> { "=== DỮ LIỆU KINH DOANH (CHỈ ĐỌC) ===" };

            if (intents.Any(i => i is ChatIntent.RevenueAnalytics or ChatIntent.SalesTrends or ChatIntent.General))
            {
                var d = await _revenueService.GetDashboardAsync(RevenueDateFilter.Today, cancellationToken: cancellationToken);
                sections.Add($"[DOANH THU] Hôm nay: {d.RevenueToday:N0} VND | Tuần: {d.RevenueThisWeek:N0} | Tháng: {d.RevenueThisMonth:N0} | Năm: {d.RevenueThisYear:N0}");
                sections.Add($"[ĐƠN HÀNG] Đã xác nhận/hoàn tất: {d.TotalConfirmedOrders} | Giá trị TB/đơn: {d.AverageOrderValue:N0} VND");
                if (d.TopSellingProducts.Count > 0)
                    sections.Add("[BÁN CHẠY] " + string.Join("; ", d.TopSellingProducts.Select(p => $"{p.ProductName}: {p.SoldQuantity} sp, {p.TotalRevenue:N0}đ")));
            }

            if (intents.Any(i => i is ChatIntent.OrderManagement or ChatIntent.General))
            {
                var pending = await _context.Orders.CountAsync(o => o.Status == OrderStatuses.Pending, cancellationToken);
                var statuses = await _context.Orders.AsNoTracking()
                    .GroupBy(o => o.Status)
                    .Select(g => $"{g.Key}={g.Count()}")
                    .ToListAsync(cancellationToken);
                sections.Add($"[ĐƠN] Chờ xử lý: {pending} | Phân bổ: {string.Join(", ", statuses)}");
            }

            if (intents.Any(i => i is ChatIntent.InventoryAnalysis or ChatIntent.General))
            {
                var low = await _context.Ingredients.AsNoTracking()
                    .Where(i => i.Quantity <= 5)
                    .OrderBy(i => i.Quantity)
                    .Take(8)
                    .Select(i => $"{i.Name} ({i.Quantity} {i.Measurement})")
                    .ToListAsync(cancellationToken);
                if (low.Count > 0)
                    sections.Add("[TỒN KHO THẤP] " + string.Join(", ", low));
            }

            if (intents.Any(i => i is ChatIntent.ProductRecommendation or ChatIntent.SalesTrends))
            {
                var allProducts = await GetTopProductsAsync(5, cancellationToken);
                sections.Add("[TOP SẢN PHẨM] " + string.Join("; ", allProducts));
            }

            return string.Join(Environment.NewLine, sections);
        }

        private async Task<string> BuildCustomerContextAsync(
            IReadOnlyList<ChatIntent> intents,
            string userMessage,
            CancellationToken cancellationToken)
        {
            var sections = new List<string> { "=== THÔNG TIN CỬA HÀNG CHO KHÁCH ===" };

            if (intents.Contains(ChatIntent.Delivery) || intents.Contains(ChatIntent.General))
            {
                sections.Add("[GIAO HÀNG] Có giao nội thành. Thời gian ước tính 2-3 ngày làm việc sau khi xác nhận đơn. Hỗ trợ giao tận nơi.");
            }

            if (intents.Contains(ChatIntent.Contact) || intents.Contains(ChatIntent.General))
            {
                sections.Add("[LIÊN HỆ] Hotline 1900-SWEET | Email support@sweetcakeshop.vn | Website SweetCakeShop.");
            }

            if (intents.Contains(ChatIntent.OrderSupport) || intents.Contains(ChatIntent.General))
            {
                sections.Add("[ĐẶT HÀNG] Chọn sản phẩm → Thêm giỏ → Thanh toán COD hoặc Online (Stripe).");
            }

            if (intents.Contains(ChatIntent.Promotions))
            {
                sections.Add("[KHUYẾN MÃI] Theo dõi fanpage/website để cập nhật ưu đãi theo mùa. Liên hệ hotline để biết chương trình hiện tại.");
            }

            if (intents.Any(i => i is ChatIntent.Pricing or ChatIntent.ProductRecommendation or ChatIntent.General))
            {
                var maxPrice = ExtractMaxPrice(userMessage);
                var products = await GetCustomerProductsAsync(maxPrice, cancellationToken);
                sections.Add("[SẢN PHẨM & GIÁ]");
                sections.AddRange(products);
            }

            if (intents.Contains(ChatIntent.ProductRecommendation))
            {
                var top = await GetTopProductsAsync(3, cancellationToken);
                sections.Add("[ĐƯỢC YÊU THÍCH] " + string.Join(", ", top));
            }

            return string.Join(Environment.NewLine, sections);
        }

        private async Task<List<string>> GetCustomerProductsAsync(decimal? maxPrice, CancellationToken cancellationToken)
        {
            var query = from p in _context.Products.AsNoTracking()
                        join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                        select new { p.ProductName, p.Price, p.Description, c.CategoryName };

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            return await query
                .OrderBy(p => p.Price)
                .Take(20)
                .Select(p => $"- {p.ProductName} ({p.CategoryName}): {p.Price:N0} VND" +
                             (string.IsNullOrWhiteSpace(p.Description) ? "" : $" — {p.Description}"))
                .ToListAsync(cancellationToken);
        }

        private async Task<List<string>> GetTopProductsAsync(int take, CancellationToken cancellationToken)
        {
            return await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                group od by p.ProductName into g
                orderby g.Sum(x => x.Quantity) descending
                select $"{g.Key} ({g.Sum(x => x.Quantity)} sp)")
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        private static decimal? ExtractMaxPrice(string message)
        {
            var m = Regex.Match(message.ToLowerInvariant(), @"(?:dưới|duoi|max|tối đa|toi da)\s*(\d+)\s*k");
            if (m.Success && decimal.TryParse(m.Groups[1].Value, out var k))
                return k * 1000;

            m = Regex.Match(message, @"(\d{3,7})\s*(?:đ|vnd|dong)?", RegexOptions.IgnoreCase);
            if (m.Success && decimal.TryParse(m.Groups[1].Value, out var v) && v >= 50000)
                return v;

            return null;
        }
    }
}
