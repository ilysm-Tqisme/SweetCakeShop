using SweetCakeShop.Constants;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public class RevenueAnalyticsService : IRevenueAnalyticsService
    {
        private readonly IRevenueService _revenueService;

        public RevenueAnalyticsService(IRevenueService revenueService) => _revenueService = revenueService;

        public async Task<RevenueFactDto> GetSnapshotAsync(CancellationToken ct = default)
        {
            var d = await _revenueService.GetDashboardAsync(RevenueDateFilter.Today, cancellationToken: ct);
            return new RevenueFactDto
            {
                Today = d.RevenueToday,
                Week = d.RevenueThisWeek,
                Month = d.RevenueThisMonth,
                Year = d.RevenueThisYear,
                Filtered = d.FilteredRevenue,
                FilterLabel = d.FilterLabel
            };
        }

        public async Task<(decimal Amount, int Orders, string Label)> GetForPeriodAsync(
            RevenueDateFilter filter,
            CancellationToken ct = default)
        {
            var d = await _revenueService.GetDashboardAsync(filter, cancellationToken: ct);
            return (d.FilteredRevenue, d.TotalConfirmedOrders, d.FilterLabel);
        }
    }
}
