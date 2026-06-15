using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public class RevenueService : IRevenueService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAnalyticsService _analyticsService;

        public RevenueService(ApplicationDbContext context, IAnalyticsService analyticsService)
        {
            _context = context;
            _analyticsService = analyticsService;
        }

        public async Task<RevenueStatisticsViewModel> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var dashboard = await GetDashboardAsync(RevenueDateFilter.Today, cancellationToken: cancellationToken);
            return new RevenueStatisticsViewModel
            {
                TodayRevenue = dashboard.RevenueToday,
                WeeklyRevenue = dashboard.RevenueThisWeek,
                MonthlyRevenue = dashboard.RevenueThisMonth,
                YearlyRevenue = dashboard.RevenueThisYear,
                TotalOrders = dashboard.TotalOrders,
                AverageOrderValue = dashboard.AverageOrderValue
            };
        }

        public async Task<RevenueDashboardViewModel> GetDashboardAsync(
            RevenueDateFilter filter,
            DateTime? customFrom = null,
            DateTime? customTo = null,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;
            var todayStart = now.Date;
            var (rangeStart, rangeEnd, label) = ResolveDateRange(filter, customFrom, customTo);

            var revenueBase = _context.Orders.AsNoTracking()
                .Where(o => o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status));

            var weekStart = GetWeekStart(todayStart);
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var yearStart = new DateTime(now.Year, 1, 1);

            var totalOrders = await revenueBase.CountAsync(cancellationToken);
            var totalRevenue = await revenueBase.SumAsync(o => o.TotalPrice, cancellationToken);
            var filteredRevenue = await SumInRangeAsync(revenueBase, rangeStart, rangeEnd, cancellationToken);

            var recentOrders = await _context.Orders.AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .Select(o => new DashboardOrderItem
                {
                    OrderId = o.OrderId,
                    CustomerName = o.CustomerName,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status,
                    ConfirmedAt = o.ConfirmedAt,
                    OrderDate = o.OrderDate
                })
                .ToListAsync(cancellationToken);

            var topProducts = await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                group new { od, p } by new { od.ProductId, p.ProductName } into g
                orderby g.Sum(x => x.od.Quantity) descending
                select new DashboardTopProductItem
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    SoldQuantity = g.Sum(x => x.od.Quantity),
                    TotalRevenue = g.Sum(x => x.od.Quantity * x.od.Price)
                })
                .Take(6)
                .ToListAsync(cancellationToken);

            return new RevenueDashboardViewModel
            {
                ActiveFilter = filter,
                RangeStart = rangeStart,
                RangeEnd = rangeEnd.AddTicks(-1),
                FilterLabel = label,
                RevenueToday = await SumInRangeAsync(revenueBase, todayStart, todayStart.AddDays(1), cancellationToken),
                RevenueThisWeek = await SumInRangeAsync(revenueBase, weekStart, todayStart.AddDays(1), cancellationToken),
                RevenueThisMonth = await SumInRangeAsync(revenueBase, monthStart, todayStart.AddDays(1), cancellationToken),
                RevenueThisYear = await SumInRangeAsync(revenueBase, yearStart, todayStart.AddDays(1), cancellationToken),
                FilteredRevenue = filteredRevenue,
                TotalOrders = totalOrders,
                TotalConfirmedOrders = totalOrders,
                AverageOrderValue = totalOrders > 0
                    ? Math.Round(totalRevenue / totalOrders, 0, MidpointRounding.AwayFromZero)
                    : 0,
                RecentOrders = recentOrders,
                TopSellingProducts = topProducts,
                Charts = await _analyticsService.BuildChartDataAsync(rangeStart, rangeEnd, cancellationToken)
            };
        }

        public (DateTime Start, DateTime EndExclusive, string Label) ResolveDateRange(
            RevenueDateFilter filter,
            DateTime? customFrom = null,
            DateTime? customTo = null)
        {
            var today = DateTime.Now.Date;
            return filter switch
            {
                RevenueDateFilter.Yesterday => (today.AddDays(-1), today, "Hôm qua"),
                RevenueDateFilter.Last7Days => (today.AddDays(-6), today.AddDays(1), "7 ngày qua"),
                RevenueDateFilter.Last30Days => (today.AddDays(-29), today.AddDays(1), "30 ngày qua"),
                RevenueDateFilter.ThisMonth => (new DateTime(today.Year, today.Month, 1), today.AddDays(1), "Tháng này"),
                RevenueDateFilter.ThisYear => (new DateTime(today.Year, 1, 1), today.AddDays(1), "Năm nay"),
                RevenueDateFilter.Custom when customFrom.HasValue && customTo.HasValue =>
                    (customFrom.Value.Date, customTo.Value.Date.AddDays(1), $"Tùy chọn ({customFrom:dd/MM/yyyy} - {customTo:dd/MM/yyyy})"),
                _ => (today, today.AddDays(1), "Hôm nay")
            };
        }

        private static DateTime GetWeekStart(DateTime todayStart)
        {
            var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek + (int)DayOfWeek.Monday);
            if (todayStart.DayOfWeek == DayOfWeek.Sunday)
                weekStart = todayStart.AddDays(-6);
            return weekStart;
        }

        private static async Task<decimal> SumInRangeAsync(
            IQueryable<Models.Order> query,
            DateTime start,
            DateTime endExclusive,
            CancellationToken cancellationToken) =>
            await query
                .Where(o => o.ConfirmedAt >= start && o.ConfirmedAt < endExclusive)
                .SumAsync(o => o.TotalPrice, cancellationToken);
    }
}
