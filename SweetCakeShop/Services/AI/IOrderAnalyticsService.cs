using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public interface IOrderAnalyticsService
    {
        Task<int> GetPendingCountAsync(CancellationToken ct = default);
        Task<OrderFactDto> GetOrderMetricsAsync(CancellationToken ct = default);
        Task<string> GetStatusBreakdownAsync(CancellationToken ct = default);
        Task<int> GetCakesSoldTodayAsync(CancellationToken ct = default);
        Task<IReadOnlyList<(string CustomerName, int OrderCount, decimal TotalSpent)>> GetTopCustomersAsync(int take = 5, CancellationToken ct = default);
        Task<(decimal ThisMonth, decimal LastMonth, decimal GrowthPercent)> GetRevenueGrowthAsync(CancellationToken ct = default);
    }
}
