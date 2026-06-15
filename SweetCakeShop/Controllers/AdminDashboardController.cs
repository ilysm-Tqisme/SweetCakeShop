using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Constants;
using SweetCakeShop.Services;

namespace SweetCakeShop.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminDashboardController : Controller
    {
        private readonly IRevenueService _revenueService;
        private readonly IExportService _exportService;

        public AdminDashboardController(IRevenueService revenueService, IExportService exportService)
        {
            _revenueService = revenueService;
            _exportService = exportService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            RevenueDateFilter filter = RevenueDateFilter.Today,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var model = await _revenueService.GetDashboardAsync(filter, from, to, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(
            RevenueDateFilter filter = RevenueDateFilter.Today,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var model = await _revenueService.GetDashboardAsync(filter, from, to, cancellationToken);
            var bytes = await _exportService.ExportExcelAsync(model, cancellationToken);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SweetCakeShop-Revenue-{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(
            RevenueDateFilter filter = RevenueDateFilter.Today,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var model = await _revenueService.GetDashboardAsync(filter, from, to, cancellationToken);
            var bytes = await _exportService.ExportPdfAsync(model, cancellationToken);
            return File(bytes, "application/pdf", $"SweetCakeShop-Revenue-{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
