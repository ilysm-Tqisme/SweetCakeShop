using System.Text;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.Chat
{
    public interface IProductCatalogForAiService
    {
        Task<string> BuildCatalogTextAsync(CancellationToken ct = default);
        Task<List<ProductFactDto>> GetAllProductsAsync(CancellationToken ct = default);
    }

    public class ProductCatalogForAiService : IProductCatalogForAiService
    {
        private readonly ApplicationDbContext _db;

        public ProductCatalogForAiService(ApplicationDbContext db) => _db = db;

        public async Task<List<ProductFactDto>> GetAllProductsAsync(CancellationToken ct = default) =>
            await (
                from p in _db.Products.AsNoTracking()
                join c in _db.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                orderby p.Price descending
                select new ProductFactDto
                {
                    ProductId = p.ProductId,
                    Name = p.ProductName,
                    Price = p.Price,
                    Category = c.CategoryName,
                    Description = p.Description,
                    ImageUrl = p.Image
                }).ToListAsync(ct);

        public async Task<string> BuildCatalogTextAsync(CancellationToken ct = default)
        {
            var items = await GetAllProductsAsync(ct);
            var sb = new StringBuilder();
            sb.AppendLine("DANH MỤC BÁNH SWEETCAKESHOP (CHỈ được dùng nguồn này):");
            foreach (var p in items)
            {
                var desc = string.IsNullOrWhiteSpace(p.Description) ? "" : $" | {p.Description}";
                sb.AppendLine($"- ID:{p.ProductId} | {p.Name} | {p.Price:N0} VND | Loại: {p.Category}{desc}");
            }
            return sb.ToString();
        }
    }
}
