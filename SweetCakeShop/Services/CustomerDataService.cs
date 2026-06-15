using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services
{
    public class CustomerDataService : ICustomerDataService
    {
        private readonly ApplicationDbContext _context;

        public CustomerDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> BuildCustomerContextAsync(CancellationToken cancellationToken = default)
        {
            var products = await (
                from p in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                orderby p.ProductName
                select new { p.ProductName, p.Price, Category = c.CategoryName })
                .Take(15)
                .ToListAsync(cancellationToken);

            var categories = await _context.Categories.AsNoTracking()
                .Select(c => c.CategoryName)
                .ToListAsync(cancellationToken);

            var topSelling = await (
                from od in _context.OrderDetails.AsNoTracking()
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                group od by p.ProductName into g
                orderby g.Sum(x => x.Quantity) descending
                select g.Key)
                .Take(3)
                .ToListAsync(cancellationToken);

            var lines = new List<string>
            {
                "SweetCakeShop - Cửa hàng bánh ngọt trực tuyến.",
                "Giao hàng: Có giao hàng trong nội thành (2-3 ngày làm việc).",
                "Liên hệ: Hotline 1900-SWEET, email support@sweetcakeshop.vn.",
                "Đặt hàng: Thêm vào giỏ → Thanh toán COD hoặc Online.",
                "Danh mục: " + string.Join(", ", categories),
            };

            if (topSelling.Count > 0)
                lines.Add("Bánh được yêu thích: " + string.Join(", ", topSelling));

            lines.Add("Sản phẩm nổi bật:");
            foreach (var p in products)
                lines.Add($"- {p.ProductName} ({p.Category}): {p.Price:N0} VND");

            return string.Join(Environment.NewLine, lines);
        }
    }
}
