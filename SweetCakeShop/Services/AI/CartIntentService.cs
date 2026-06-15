using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public class CartIntentService : ICartIntentService
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cart;

        public CartIntentService(ApplicationDbContext context, CartService cart)
        {
            _context = context;
            _cart = cart;
        }

        public async Task<bool> TryAddFocusedProductAsync(
            string userMessage,
            ConversationFocus? focus,
            AiBusinessContextDto context,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(focus?.ProductName))
                return false;

            var product = await _context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductName == focus.ProductName, ct);

            if (product == null)
                product = await _context.Products.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProductName.Contains(focus.ProductName), ct);

            if (product == null) return false;

            var qty = ChatIntentHelper.ExtractQuantity(userMessage, 1);
            _cart.AddToCart(product, qty);

            context.HasData = true;
            context.FocusProductName = product.ProductName;
            context.FocusProductPrice = product.Price;
            context.Facts.Add(new ContextFact
            {
                Key = "CartAction",
                Value = $"Added {qty} x {product.ProductName} ({product.Price:N0} VND each) to session cart."
            });
            context.Cart = new CartFactDto
            {
                ItemCount = _cart.GetCart().Items.Sum(i => i.Quantity),
                Total = _cart.GetCart().TotalAmount
            };
            return true;
        }
    }
}
