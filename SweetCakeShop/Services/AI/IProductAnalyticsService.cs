using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public interface IProductAnalyticsService
    {
        Task<ProductFactDto?> GetHighestPriceAsync(CancellationToken ct = default);
        Task<ProductFactDto?> GetLowestPriceAsync(CancellationToken ct = default);
        Task<IReadOnlyList<ProductFactDto>> GetTopSellingAsync(int take = 5, CancellationToken ct = default);
        Task<IReadOnlyList<ProductFactDto>> GetWorstSellingAsync(int take = 3, CancellationToken ct = default);
        Task<IReadOnlyList<ProductFactDto>> GetCatalogAsync(int take = 12, CancellationToken ct = default);
        Task<ProductFactDto?> FindByNameAsync(string name, CancellationToken ct = default);
        Task<IReadOnlyList<ProductFactDto>> SearchProductsAsync(string query, int take = 8, CancellationToken ct = default);
        Task<IReadOnlyList<ProductFactDto>> GetRelatedProductsAsync(string productName, int take = 6, CancellationToken ct = default);
        Task<ProductFactDto?> GetProductDetailsAsync(string productName, CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);
    }
}
