using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public class OrderAnalyticsService : IOrderAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevenueService _revenueService;

        public OrderAnalyticsService(ApplicationDbContext context, IRevenueService revenueService)
        {
            _context = context;
            _revenueService = revenueService;
        }

        public Task<int> GetPendingCountAsync(CancellationToken ct = default) =>
            _context.Orders.CountAsync(o => o.Status == OrderStatuses.Pending, ct);

        public async Task<OrderFactDto> GetOrderMetricsAsync(CancellationToken ct = default)
        {
            var d = await _revenueService.GetDashboardAsync(RevenueDateFilter.Today, cancellationToken: ct);
            var cakes = await GetCakesSoldTodayAsync(ct);
            return new OrderFactDto
            {
                TotalConfirmed = d.TotalConfirmedOrders,
                AverageOrderValue = d.AverageOrderValue,
                Pending = await GetPendingCountAsync(ct),
                CakesSoldToday = cakes
            };
        }

        public async Task<string> GetStatusBreakdownAsync(CancellationToken ct = default)
        {
            var statuses = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(ct);
            return string.Join(", ", statuses.Select(s => $"{s.Key}: {s.Count}"));
        }

        public async Task<int> GetCakesSoldTodayAsync(CancellationToken ct = default)
        {
            var today = DateTime.Now.Date;
            var tomorrow = today.AddDays(1);
            return await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                where o.ConfirmedAt >= today && o.ConfirmedAt < tomorrow
                      && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                select od.Quantity).SumAsync(ct);
        }

        public async Task<IReadOnlyList<(string CustomerName, int OrderCount, decimal TotalSpent)>> GetTopCustomersAsync(
            int take = 5,
            CancellationToken ct = default)
        {
            var list = await _context.Orders.AsNoTracking()
                .Where(o => o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .Where(o => !string.IsNullOrWhiteSpace(o.CustomerName))
                .GroupBy(o => o.CustomerName)
                .Select(g => new
                {
                    CustomerName = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(take)
                .ToListAsync(ct);

            return list.Select(x => (x.CustomerName, x.OrderCount, x.TotalSpent)).ToList();
        }

        public async Task<(decimal ThisMonth, decimal LastMonth, decimal GrowthPercent)> GetRevenueGrowthAsync(CancellationToken ct = default)
        {
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var nextMonth = thisMonthStart.AddMonths(1);

            var thisMonth = await _context.Orders.AsNoTracking()
                .Where(o => o.ConfirmedAt >= thisMonthStart && o.ConfirmedAt < nextMonth
                            && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .SumAsync(o => o.TotalPrice, ct);

            var lastMonth = await _context.Orders.AsNoTracking()
                .Where(o => o.ConfirmedAt >= lastMonthStart && o.ConfirmedAt < thisMonthStart
                            && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .SumAsync(o => o.TotalPrice, ct);

            var growth = lastMonth > 0 ? (thisMonth - lastMonth) / lastMonth * 100 : 0;
            return (thisMonth, lastMonth, growth);
        }
    }
}
