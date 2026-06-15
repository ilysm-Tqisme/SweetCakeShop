using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChartDataViewModel> BuildChartDataAsync(
            DateTime rangeStart,
            DateTime rangeEndExclusive,
            CancellationToken cancellationToken = default)
        {
            var revenueOrders = _context.Orders.AsNoTracking()
                .Where(o => o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status));

            var days = Math.Max(1, (int)(rangeEndExclusive - rangeStart).TotalDays);
            if (days > 31) days = 31;

            var lineLabels = new List<string>();
            var lineValues = new List<decimal>();
            for (var i = 0; i < days; i++)
            {
                var day = rangeStart.AddDays(i);
                if (day >= rangeEndExclusive) break;
                lineLabels.Add(day.ToString("dd/MM"));
                var dayRevenue = await revenueOrders
                    .Where(o => o.ConfirmedAt >= day && o.ConfirmedAt < day.AddDays(1))
                    .SumAsync(o => o.TotalPrice, cancellationToken);
                lineValues.Add(dayRevenue);
            }

            var year = DateTime.Now.Year;
            var monthlyLabels = new List<string>();
            var monthlyValues = new List<decimal>();
            for (var m = 1; m <= 12; m++)
            {
                var start = new DateTime(year, m, 1);
                var end = start.AddMonths(1);
                monthlyLabels.Add($"T{m}");
                monthlyValues.Add(await revenueOrders
                    .Where(o => o.ConfirmedAt >= start && o.ConfirmedAt < end)
                    .SumAsync(o => o.TotalPrice, cancellationToken));
            }

            var statusGroups = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var topProducts = await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                group od by p.ProductName into g
                orderby g.Sum(x => x.Quantity) descending
                select new { Name = g.Key, Revenue = g.Sum(x => x.Quantity * x.Price) })
                .Take(5)
                .ToListAsync(cancellationToken);

            return new ChartDataViewModel
            {
                RevenueLineLabels = lineLabels,
                RevenueLineValues = lineValues,
                MonthlyBarLabels = monthlyLabels,
                MonthlyBarValues = monthlyValues,
                OrderStatusLabels = statusGroups.Select(x => x.Status).ToList(),
                OrderStatusValues = statusGroups.Select(x => x.Count).ToList(),
                TopProductLabels = topProducts.Select(x => x.Name).ToList(),
                TopProductValues = topProducts.Select(x => x.Revenue).ToList()
            };
        }
    }
}
