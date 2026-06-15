using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context) => _context = context;

        public async Task<IReadOnlyList<InventoryFactDto>> GetLowStockAsync(int take = 6, CancellationToken ct = default)
        {
            return await _context.Ingredients.AsNoTracking()
                .Where(i => i.Quantity <= 5)
                .OrderBy(i => i.Quantity)
                .Take(take)
                .Select(i => new InventoryFactDto
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Unit = i.Measurement
                })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<InventoryFactDto>> GetAllIngredientsAsync(int take = 20, CancellationToken ct = default) =>
            await _context.Ingredients.AsNoTracking()
                .OrderBy(i => i.Name)
                .Take(take)
                .Select(i => new InventoryFactDto
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Unit = i.Measurement
                })
                .ToListAsync(ct);
    }
}
