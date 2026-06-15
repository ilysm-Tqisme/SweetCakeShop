using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Database-backed cart service for authenticated users.
    /// Merges session cart into DB on login, and syncs DB→session on page load.
    /// </summary>
    public interface IDbCartService
    {
        /// <summary>Get the DB-backed cart for a user.</summary>
        Task<CartViewModel> GetCartAsync(string userId);

        /// <summary>Add or increment a product in the DB cart.</summary>
        Task AddToCartAsync(string userId, int productId, int quantity = 1);

        /// <summary>Update quantity of a product in the DB cart.</summary>
        Task UpdateQuantityAsync(string userId, int productId, int quantity);

        /// <summary>Remove a product from the DB cart.</summary>
        Task RemoveFromCartAsync(string userId, int productId);

        /// <summary>Clear all items from the DB cart.</summary>
        Task ClearCartAsync(string userId);

        /// <summary>
        /// Merge session cart items into DB cart (summing quantities).
        /// Called on login to preserve anonymous cart items.
        /// </summary>
        Task MergeSessionCartAsync(string userId, CartViewModel sessionCart);

        /// <summary>Get item count for badge display.</summary>
        Task<int> GetItemCountAsync(string userId);
    }

    public class DbCartService : IDbCartService
    {
        private readonly ApplicationDbContext _context;

        public DbCartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CartViewModel> GetCartAsync(string userId)
        {
            var items = await _context.CartItems
                .AsNoTracking()
                .Where(ci => ci.UserId == userId)
                .Include(ci => ci.Product)
                .OrderByDescending(ci => ci.AddedAt)
                .ToListAsync();

            var vm = new CartViewModel();
            foreach (var item in items)
            {
                if (item.Product == null) continue;
                vm.Items.Add(new CartItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.ProductName,
                    Price = item.Product.Price,
                    Image = item.Product.Image,
                    Quantity = item.Quantity
                });
            }
            return vm;
        }

        public async Task AddToCartAsync(string userId, int productId, int quantity = 1)
        {
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.UpdatedAt = DateTime.Now;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(string userId, int productId, int quantity)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);

            if (item == null) return;

            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                item.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(string userId, int productId)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string userId)
        {
            var items = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .ToListAsync();

            if (items.Count > 0)
            {
                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
        }

        public async Task MergeSessionCartAsync(string userId, CartViewModel sessionCart)
        {
            if (sessionCart.Items.Count == 0) return;

            var dbItems = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .ToListAsync();

            foreach (var sessionItem in sessionCart.Items)
            {
                var existing = dbItems.FirstOrDefault(d => d.ProductId == sessionItem.ProductId);

                if (existing != null)
                {
                    // Merge strategy: sum quantities (as approved)
                    existing.Quantity += sessionItem.Quantity;
                    existing.UpdatedAt = DateTime.Now;
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        UserId = userId,
                        ProductId = sessionItem.ProductId,
                        Quantity = sessionItem.Quantity,
                        AddedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetItemCountAsync(string userId)
        {
            return await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .SumAsync(ci => ci.Quantity);
        }
    }
}
