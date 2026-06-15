using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Smart search with Vietnamese normalization, fuzzy matching,
    /// and relevance-based ranking.
    /// </summary>
    public interface ISmartSearchService
    {
        /// <summary>
        /// Search products with smart Vietnamese-aware matching.
        /// Returns results sorted by relevance score descending.
        /// </summary>
        Task<List<ProductSearchResult>> SearchAsync(string? query, int maxResults = 50);

        /// <summary>
        /// Autocomplete suggestions for search bar.
        /// </summary>
        Task<List<string>> AutocompleteAsync(string? query, int maxResults = 8);
    }

    public class ProductSearchResult
    {
        public Product Product { get; set; } = null!;
        public double RelevanceScore { get; set; }
    }

    public class SmartSearchService : ISmartSearchService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVietnameseNormalizerService _normalizer;

        public SmartSearchService(ApplicationDbContext context, IVietnameseNormalizerService normalizer)
        {
            _context = context;
            _normalizer = normalizer;
        }

        public async Task<List<ProductSearchResult>> SearchAsync(string? query, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                var allProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .OrderBy(p => p.ProductId)
                    .Take(maxResults)
                    .ToListAsync();

                return allProducts.Select(p => new ProductSearchResult
                {
                    Product = p,
                    RelevanceScore = 0.0
                }).ToList();
            }

            // Load all products and score client-side for fuzzy matching
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .ToListAsync();

            var results = new List<ProductSearchResult>();

            foreach (var product in products)
            {
                // Score against product name (weight: 1.0)
                var nameScore = _normalizer.Score(product.ProductName, query);

                // Score against description (weight: 0.5)
                var descScore = _normalizer.Score(product.Description, query) * 0.5;

                // Score against category name (weight: 0.3)
                var catScore = _normalizer.Score(product.Category?.CategoryName, query) * 0.3;

                var totalScore = Math.Max(nameScore, Math.Max(descScore, catScore));

                if (totalScore > 0.0)
                {
                    results.Add(new ProductSearchResult
                    {
                        Product = product,
                        RelevanceScore = totalScore
                    });
                }
            }

            return results
                .OrderByDescending(r => r.RelevanceScore)
                .Take(maxResults)
                .ToList();
        }

        public async Task<List<string>> AutocompleteAsync(string? query, int maxResults = 8)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<string>();

            var products = await _context.Products
                .AsNoTracking()
                .Select(p => p.ProductName)
                .ToListAsync();

            return products
                .Where(name => _normalizer.FuzzyContains(name, query))
                .OrderBy(name => name)
                .Take(maxResults)
                .ToList();
        }
    }
}
