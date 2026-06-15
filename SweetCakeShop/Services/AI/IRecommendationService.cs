using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public interface IRecommendationService
    {
        Task<IReadOnlyList<ProductFactDto>> RecommendAsync(string userMessage, CancellationToken ct = default);

        Task<IReadOnlyList<ProductFactDto>> RecommendWithPreferencesAsync(
            string? occasion,
            string? flavor,
            decimal? maxPrice,
            int limit = 8,
            CancellationToken ct = default);
    }
}
