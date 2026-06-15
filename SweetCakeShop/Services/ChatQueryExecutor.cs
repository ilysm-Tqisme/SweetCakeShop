using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public class ChatQueryExecutor : IChatQueryExecutor
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevenueService _revenueService;

        public ChatQueryExecutor(ApplicationDbContext context, IRevenueService revenueService)
        {
            _context = context;
            _revenueService = revenueService;
        }

        public async Task<ChatQueryResult> ExecuteAsync(
            AiChatMode mode,
            ChatQueryAction action,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            var result = new ChatQueryResult
            {
                Action = action,
                UserQuestion = userMessage.Trim()
            };

            switch (action)
            {
                case ChatQueryAction.GetHighestPriceProduct:
                    await FillHighestPriceAsync(result, cancellationToken);
                    break;
                case ChatQueryAction.GetLowestPriceProduct:
                    await FillLowestPriceAsync(result, cancellationToken);
                    break;
                case ChatQueryAction.GetTopSellingProducts:
                    await FillTopSellingAsync(result, 5, cancellationToken);
                    break;
                case ChatQueryAction.GetWorstSellingProducts:
                    await FillWorstSellingAsync(result, cancellationToken);
                    break;
                case ChatQueryAction.SearchProductsByBudget:
                    await FillProductsByBudgetAsync(result, userMessage, cancellationToken);
                    break;
                case ChatQueryAction.GetDeliveryInfo:
                    FillDelivery(result);
                    break;
                case ChatQueryAction.GetOrderGuide:
                    FillOrderGuide(result);
                    break;
                case ChatQueryAction.GetPromotionInfo:
                    FillPromotion(result);
                    break;
                case ChatQueryAction.GetContactInfo:
                    FillContact(result);
                    break;
                case ChatQueryAction.GetRevenueToday:
                    await FillRevenueAsync(result, RevenueDateFilter.Today, cancellationToken);
                    break;
                case ChatQueryAction.GetRevenueWeek:
                    await FillRevenueAsync(result, RevenueDateFilter.Last7Days, cancellationToken);
                    break;
                case ChatQueryAction.GetRevenueMonth:
                    await FillRevenueAsync(result, RevenueDateFilter.ThisMonth, cancellationToken);
                    break;
                case ChatQueryAction.GetRevenueYear:
                    await FillRevenueAsync(result, RevenueDateFilter.ThisYear, cancellationToken);
                    break;
                case ChatQueryAction.GetRevenueSummary:
                    await FillRevenueSummaryAsync(result, cancellationToken);
                    break;
                case ChatQueryAction.GetPendingOrders:
                    await FillPendingOrdersAsync(result, cancellationToken);
                    break;
                case ChatQueryAction.GetOrderStatusSummary:
                    await FillOrderStatusAsync(result, cancellationToken);
                    break;
                case ChatQueryAction.GetLowStockIngredients:
                    await FillLowStockAsync(result, cancellationToken);
                    break;
                case ChatQueryAction.GetCakesSoldToday:
                    await FillCakesSoldTodayAsync(result, cancellationToken);
                    break;
                default:
                    if (mode == AiChatMode.Admin)
                        await FillRevenueSummaryAsync(result, cancellationToken);
                    else
                        await FillProductCatalogAsync(result, 12, cancellationToken);
                    break;
            }

            result.HasData = result.Facts.Count > 0;
            return result;
        }

        private async Task FillHighestPriceAsync(ChatQueryResult r, CancellationToken ct)
        {
            var p = await (
                from prod in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on prod.CategoryId equals c.CategoryId
                orderby prod.Price descending
                select new { prod.ProductName, prod.Price, c.CategoryName, prod.Description })
                .FirstOrDefaultAsync(ct);

            if (p == null) return;
            r.Facts["ten_banh"] = p.ProductName;
            r.Facts["gia"] = $"{p.Price:N0} VND";
            r.Facts["danh_muc"] = p.CategoryName;
            if (!string.IsNullOrWhiteSpace(p.Description))
                r.Facts["mo_ta"] = p.Description!;
        }

        private async Task FillLowestPriceAsync(ChatQueryResult r, CancellationToken ct)
        {
            var p = await _context.Products.AsNoTracking()
                .OrderBy(p => p.Price)
                .Select(p => new { p.ProductName, p.Price })
                .FirstOrDefaultAsync(ct);
            if (p == null) return;
            r.Facts["ten_banh"] = p.ProductName;
            r.Facts["gia"] = $"{p.Price:N0} VND";
        }

        private async Task FillTopSellingAsync(ChatQueryResult r, int take, CancellationToken ct)
        {
            var items = await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                group od by p.ProductName into g
                orderby g.Sum(x => x.Quantity) descending
                select new { Name = g.Key, Qty = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.Quantity * x.Price) })
                .Take(take)
                .ToListAsync(ct);

            for (var i = 0; i < items.Count; i++)
                r.Facts[$"top_{i + 1}"] = $"{items[i].Name} — {items[i].Qty} sp bán, doanh thu {items[i].Revenue:N0}đ";
            if (items.Count > 0)
                r.Facts["ban_chay_nhat"] = items[0].Name;
        }

        private async Task FillWorstSellingAsync(ChatQueryResult r, CancellationToken ct)
        {
            var items = await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                group od by p.ProductName into g
                orderby g.Sum(x => x.Quantity)
                select new { Name = g.Key, Qty = g.Sum(x => x.Quantity) })
                .Take(3)
                .ToListAsync(ct);

            for (var i = 0; i < items.Count; i++)
                r.Facts[$"thap_{i + 1}"] = $"{items[i].Name} — {items[i].Qty} sp";
        }

        private async Task FillProductsByBudgetAsync(ChatQueryResult r, string message, CancellationToken ct)
        {
            var max = ExtractMaxPrice(message);
            var q = from p in _context.Products.AsNoTracking()
                    join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                    select new { p.ProductName, p.Price, c.CategoryName };
            if (max.HasValue) q = q.Where(p => p.Price <= max.Value);
            var list = await q.OrderBy(p => p.Price).Take(8).ToListAsync(ct);
            for (var i = 0; i < list.Count; i++)
                r.Facts[$"goi_y_{i + 1}"] = $"{list[i].ProductName} ({list[i].CategoryName}): {list[i].Price:N0}đ";
        }

        private async Task FillProductCatalogAsync(ChatQueryResult r, int take, CancellationToken ct)
        {
            var list = await (
                from p in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                orderby p.ProductName
                select new { p.ProductName, p.Price, c.CategoryName })
                .Take(take)
                .ToListAsync(ct);
            for (var i = 0; i < list.Count; i++)
                r.Facts[$"sp_{i + 1}"] = $"{list[i].ProductName} ({list[i].CategoryName}): {list[i].Price:N0}đ";
        }

        private static void FillDelivery(ChatQueryResult r)
        {
            r.Facts["giao_hang"] = "Có giao hàng nội thành";
            r.Facts["thoi_gian"] = "2-3 ngày làm việc sau khi xác nhận đơn";
            r.Facts["ho_tro"] = "Giao tận nơi, phí ship theo khu vực";
        }

        private static void FillOrderGuide(ChatQueryResult r)
        {
            r.Facts["cach_dat"] = "Chọn sản phẩm → Thêm vào giỏ → Thanh toán COD hoặc Online";
            r.Facts["thanh_toan"] = "COD hoặc Stripe Online";
        }

        private static void FillPromotion(ChatQueryResult r)
        {
            r.Facts["khuyen_mai"] = "Ưu đãi theo mùa trên website và fanpage";
            r.Facts["lien_he"] = "Gọi 1900-SWEET để biết chương trình hiện tại";
        }

        private static void FillContact(ChatQueryResult r)
        {
            r.Facts["hotline"] = "1900-SWEET";
            r.Facts["email"] = "support@sweetcakeshop.vn";
        }

        private async Task FillRevenueAsync(ChatQueryResult r, RevenueDateFilter filter, CancellationToken ct)
        {
            var d = await _revenueService.GetDashboardAsync(filter, cancellationToken: ct);
            r.Facts["doanh_thu_loc"] = $"{d.FilteredRevenue:N0} VND";
            r.Facts["bo_loc"] = d.FilterLabel;
            r.Facts["so_don"] = d.TotalConfirmedOrders;
        }

        private async Task FillRevenueSummaryAsync(ChatQueryResult r, CancellationToken ct)
        {
            var d = await _revenueService.GetDashboardAsync(RevenueDateFilter.Today, cancellationToken: ct);
            r.Facts["doanh_thu_hom_nay"] = $"{d.RevenueToday:N0} VND";
            r.Facts["doanh_thu_tuan"] = $"{d.RevenueThisWeek:N0} VND";
            r.Facts["doanh_thu_thang"] = $"{d.RevenueThisMonth:N0} VND";
            r.Facts["doanh_thu_nam"] = $"{d.RevenueThisYear:N0} VND";
            r.Facts["don_xac_nhan"] = d.TotalConfirmedOrders;
            r.Facts["gia_tri_tb_don"] = $"{d.AverageOrderValue:N0} VND";
            if (d.TopSellingProducts.Count > 0)
                r.Facts["ban_chay_nhat"] = d.TopSellingProducts[0].ProductName;
        }

        private async Task FillPendingOrdersAsync(ChatQueryResult r, CancellationToken ct)
        {
            var count = await _context.Orders.CountAsync(o => o.Status == OrderStatuses.Pending, ct);
            r.Facts["don_cho"] = count;
        }

        private async Task FillOrderStatusAsync(ChatQueryResult r, CancellationToken ct)
        {
            var statuses = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(ct);
            foreach (var s in statuses)
                r.Facts[$"trang_thai_{s.Key}"] = s.Count;
        }

        private async Task FillLowStockAsync(ChatQueryResult r, CancellationToken ct)
        {
            var items = await _context.Ingredients.AsNoTracking()
                .Where(i => i.Quantity <= 5)
                .OrderBy(i => i.Quantity)
                .Take(6)
                .ToListAsync(ct);
            for (var i = 0; i < items.Count; i++)
                r.Facts[$"ton_kho_{i + 1}"] = $"{items[i].Name}: {items[i].Quantity} {items[i].Measurement}";
        }

        private async Task FillCakesSoldTodayAsync(ChatQueryResult r, CancellationToken ct)
        {
            var today = DateTime.Now.Date;
            var tomorrow = today.AddDays(1);
            var qty = await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                where o.ConfirmedAt >= today && o.ConfirmedAt < tomorrow
                      && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                select od.Quantity).SumAsync(ct);

            var revenue = await _context.Orders.AsNoTracking()
                .Where(o => o.ConfirmedAt >= today && o.ConfirmedAt < tomorrow
                            && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .SumAsync(o => o.TotalPrice, ct);

            r.Facts["so_banh_ban_hom_nay"] = qty;
            r.Facts["doanh_thu_hom_nay"] = $"{revenue:N0} VND";
        }

        private static decimal? ExtractMaxPrice(string message)
        {
            var m = Regex.Match(message.ToLowerInvariant(), @"(?:duoi|dưới|max|toi da|tối đa)\s*(\d+)\s*k");
            if (m.Success && decimal.TryParse(m.Groups[1].Value, out var k)) return k * 1000;
            m = Regex.Match(message, @"(\d{3,7})\s*(?:đ|vnd)?", RegexOptions.IgnoreCase);
            if (m.Success && decimal.TryParse(m.Groups[1].Value, out var v) && v >= 50000) return v;
            return null;
        }
    }
}
