using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportExcelAsync(RevenueDashboardViewModel model, CancellationToken cancellationToken = default);
        Task<byte[]> ExportPdfAsync(RevenueDashboardViewModel model, CancellationToken cancellationToken = default);
    }
}
