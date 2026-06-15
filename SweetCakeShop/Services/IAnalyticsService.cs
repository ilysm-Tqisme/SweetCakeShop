using SweetCakeShop.Constants;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public interface IAnalyticsService
    {
        Task<ChartDataViewModel> BuildChartDataAsync(
            DateTime rangeStart,
            DateTime rangeEndExclusive,
            CancellationToken cancellationToken = default);
    }
}
