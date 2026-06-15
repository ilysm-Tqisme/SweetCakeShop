using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public class ExportService : IExportService
    {
        static ExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public Task<byte[]> ExportExcelAsync(RevenueDashboardViewModel model, CancellationToken cancellationToken = default)
        {
            using var workbook = new XLWorkbook();

            var summary = workbook.Worksheets.Add("Tổng quan");
            summary.Cell(1, 1).Value = "SweetCakeShop - Báo cáo doanh thu";
            summary.Cell(2, 1).Value = "Bộ lọc"; summary.Cell(2, 2).Value = model.FilterLabel;
            summary.Cell(3, 1).Value = "Từ ngày"; summary.Cell(3, 2).Value = model.RangeStart.ToString("dd/MM/yyyy");
            summary.Cell(4, 1).Value = "Đến ngày"; summary.Cell(4, 2).Value = model.RangeEnd.ToString("dd/MM/yyyy");
            summary.Cell(6, 1).Value = "Doanh thu hôm nay"; summary.Cell(6, 2).Value = model.RevenueToday;
            summary.Cell(7, 1).Value = "Doanh thu tuần"; summary.Cell(7, 2).Value = model.RevenueThisWeek;
            summary.Cell(8, 1).Value = "Doanh thu tháng"; summary.Cell(8, 2).Value = model.RevenueThisMonth;
            summary.Cell(9, 1).Value = "Doanh thu năm"; summary.Cell(9, 2).Value = model.RevenueThisYear;
            summary.Cell(10, 1).Value = "Doanh thu theo bộ lọc"; summary.Cell(10, 2).Value = model.FilteredRevenue;
            summary.Cell(11, 1).Value = "Tổng đơn xác nhận"; summary.Cell(11, 2).Value = model.TotalConfirmedOrders;
            summary.Cell(12, 1).Value = "Giá trị TB/đơn"; summary.Cell(12, 2).Value = model.AverageOrderValue;

            var ordersSheet = workbook.Worksheets.Add("Đơn hàng gần đây");
            ordersSheet.Cell(1, 1).Value = "Mã đơn";
            ordersSheet.Cell(1, 2).Value = "Khách hàng";
            ordersSheet.Cell(1, 3).Value = "Tổng tiền";
            ordersSheet.Cell(1, 4).Value = "Trạng thái";
            ordersSheet.Cell(1, 5).Value = "Ngày xác nhận";
            var row = 2;
            foreach (var o in model.RecentOrders)
            {
                ordersSheet.Cell(row, 1).Value = o.OrderId;
                ordersSheet.Cell(row, 2).Value = o.CustomerName;
                ordersSheet.Cell(row, 3).Value = o.TotalPrice;
                ordersSheet.Cell(row, 4).Value = o.Status;
                ordersSheet.Cell(row, 5).Value = o.ConfirmedAt?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                row++;
            }

            var productsSheet = workbook.Worksheets.Add("Bánh bán chạy");
            productsSheet.Cell(1, 1).Value = "Tên bánh";
            productsSheet.Cell(1, 2).Value = "SL bán";
            productsSheet.Cell(1, 3).Value = "Doanh thu";
            row = 2;
            foreach (var p in model.TopSellingProducts)
            {
                productsSheet.Cell(row, 1).Value = p.ProductName;
                productsSheet.Cell(row, 2).Value = p.SoldQuantity;
                productsSheet.Cell(row, 3).Value = p.TotalRevenue;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return Task.FromResult(stream.ToArray());
        }

        public Task<byte[]> ExportPdfAsync(RevenueDashboardViewModel model, CancellationToken cancellationToken = default)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text("SweetCakeShop - Báo cáo doanh thu")
                        .Bold().FontSize(18).FontColor(Colors.Pink.Medium);

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Text($"Bộ lọc: {model.FilterLabel}");
                        col.Item().Text($"Khoảng: {model.RangeStart:dd/MM/yyyy} - {model.RangeEnd:dd/MM/yyyy}");
                        col.Item().Text($"Doanh thu hôm nay: {model.RevenueToday:N0} VND");
                        col.Item().Text($"Doanh thu tuần: {model.RevenueThisWeek:N0} VND");
                        col.Item().Text($"Doanh thu tháng: {model.RevenueThisMonth:N0} VND");
                        col.Item().Text($"Doanh thu năm: {model.RevenueThisYear:N0} VND");
                        col.Item().Text($"Doanh thu (lọc): {model.FilteredRevenue:N0} VND");
                        col.Item().Text($"Tổng đơn xác nhận: {model.TotalConfirmedOrders}");
                        col.Item().Text($"Giá trị TB/đơn: {model.AverageOrderValue:N0} VND");

                        col.Item().PaddingTop(10).Text("Bánh bán chạy").Bold();
                        foreach (var p in model.TopSellingProducts)
                            col.Item().Text($"- {p.ProductName}: {p.SoldQuantity} sp, {p.TotalRevenue:N0} VND");

                        col.Item().PaddingTop(10).Text("Đơn hàng gần đây").Bold();
                        foreach (var o in model.RecentOrders.Take(10))
                            col.Item().Text($"#{o.OrderId} {o.CustomerName} - {o.TotalPrice:N0} VND ({o.Status})");
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("SweetCakeShop © ");
                        txt.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    });
                });
            });

            using var ms = new MemoryStream();
            document.GeneratePdf(ms);
            return Task.FromResult(ms.ToArray());
        }
    }
}
