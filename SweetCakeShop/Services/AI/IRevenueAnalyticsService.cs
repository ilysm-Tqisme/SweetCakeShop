using SweetCakeShop.Constants;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public interface IRevenueAnalyticsService
    {
        Task<RevenueFactDto> GetSnapshotAsync(CancellationToken ct = default);
        Task<(decimal Amount, int Orders, string Label)> GetForPeriodAsync(RevenueDateFilter filter, CancellationToken ct = default);
    }
}
