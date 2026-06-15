using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services
{
    public class AiBusinessDataService : IAiBusinessDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevenueService _revenueService;

        public AiBusinessDataService(ApplicationDbContext context, IRevenueService revenueService)
        {
            _context = context;
            _revenueService = revenueService;
        }

        public async Task<string> BuildAdminContextAsync(CancellationToken cancellationToken = default)
        {
            var dashboard = await _revenueService.GetDashboardAsync(RevenueDateFilter.Today, cancellationToken: cancellationToken);

            var orderStatusCounts = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var pendingOrders = await _context.Orders.AsNoTracking()
                .CountAsync(o => o.Status == OrderStatuses.Pending, cancellationToken);

            var lowStock = await _context.Ingredients.AsNoTracking()
                .Where(i => i.Quantity <= 5)
                .OrderBy(i => i.Quantity)
                .Take(5)
                .Select(i => $"{i.Name}: {i.Quantity} {i.Measurement}")
                .ToListAsync(cancellationToken);

            var lines = new List<string>
            {
                "=== DỮ LIỆU SWEETCAKESHOP (CHỈ ĐỌC) ===",
                $"Doanh thu hôm nay: {dashboard.RevenueToday:N0} VND",
                $"Doanh thu tuần: {dashboard.RevenueThisWeek:N0} VND",
                $"Doanh thu tháng: {dashboard.RevenueThisMonth:N0} VND",
                $"Doanh thu năm: {dashboard.RevenueThisYear:N0} VND",
                $"Đơn đã xác nhận/hoàn tất: {dashboard.TotalConfirmedOrders}",
                $"Giá trị đơn trung bình: {dashboard.AverageOrderValue:N0} VND",
                $"Đơn chờ xử lý (Pending): {pendingOrders}",
                "Trạng thái đơn: " + string.Join(", ", orderStatusCounts.Select(x => $"{x.Status}={x.Count}"))
            };

            if (dashboard.TopSellingProducts.Count > 0)
            {
                lines.Add("Bánh bán chạy: " + string.Join(", ",
                    dashboard.TopSellingProducts.Select(p => $"{p.ProductName} ({p.SoldQuantity} sp)")));
            }

            if (lowStock.Count > 0)
                lines.Add("Nguyên liệu sắp hết: " + string.Join(", ", lowStock));

            return string.Join(Environment.NewLine, lines);
        }
    }
}
