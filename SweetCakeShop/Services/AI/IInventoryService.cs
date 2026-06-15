using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public interface IInventoryService
    {
        Task<IReadOnlyList<InventoryFactDto>> GetLowStockAsync(int take = 6, CancellationToken ct = default);
        Task<IReadOnlyList<InventoryFactDto>> GetAllIngredientsAsync(int take = 20, CancellationToken ct = default);
    }
}
