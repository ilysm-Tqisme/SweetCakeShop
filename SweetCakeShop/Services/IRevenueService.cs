using SweetCakeShop.Constants;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public interface IRevenueService
    {
        Task<RevenueStatisticsViewModel> GetStatisticsAsync(CancellationToken cancellationToken = default);
        Task<RevenueDashboardViewModel> GetDashboardAsync(
            RevenueDateFilter filter,
            DateTime? customFrom = null,
            DateTime? customTo = null,
            CancellationToken cancellationToken = default);

        (DateTime Start, DateTime EndExclusive, string Label) ResolveDateRange(
            RevenueDateFilter filter,
            DateTime? customFrom = null,
            DateTime? customTo = null);
    }
}
