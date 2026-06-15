using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;

        public RecommendationService(ApplicationDbContext context) => _context = context;

        public Task<IReadOnlyList<ProductFactDto>> RecommendAsync(string userMessage, CancellationToken ct = default) =>
            RecommendWithPreferencesAsync(null, null, null, 8, ct);

        public async Task<IReadOnlyList<ProductFactDto>> RecommendWithPreferencesAsync(
            string? occasion,
            string? flavor,
            decimal? maxPrice,
            int limit = 8,
            CancellationToken ct = default)
        {
            var q = from p in _context.Products.AsNoTracking()
                    join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                    select new { p.ProductName, p.Price, p.Description, c.CategoryName };

            if (maxPrice.HasValue)
                q = q.Where(x => x.Price <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(flavor))
            {
                var f = flavor.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    x.ProductName.ToLower().Contains(f) ||
                    (x.Description != null && x.Description.ToLower().Contains(f)) ||
                    x.CategoryName.ToLower().Contains(f));
            }

            if (!string.IsNullOrWhiteSpace(occasion))
            {
                var o = occasion.Trim().ToLowerInvariant();
                if (o.Contains("birthday") || o.Contains("sinh nhật") || o.Contains("sinh nhat"))
                    q = q.OrderByDescending(x => x.Price);
                else if (o.Contains("wedding") || o.Contains("cưới") || o.Contains("cuoi"))
                    q = q.OrderByDescending(x => x.Price);
                else if (o.Contains("gift") || o.Contains("quà") || o.Contains("qua") || o.Contains("girlfriend") || o.Contains("người yêu"))
                    q = q.OrderByDescending(x => x.Price);
            }

            var list = await q.Take(limit)
                .Select(x => new ProductFactDto
                {
                    Name = x.ProductName,
                    Price = x.Price,
                    Category = x.CategoryName,
                    Description = x.Description
                }).ToListAsync(ct);

            if (list.Count > 0) return list;

            return await (
                from p in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                orderby p.Price
                select new ProductFactDto { Name = p.ProductName, Price = p.Price, Category = c.CategoryName })
                .Take(limit).ToListAsync(ct);
        }
    }
}
