using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services
{
    public class ChatPageContextService : IChatPageContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly CartService _cart;

        public ChatPageContextService(
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context,
            CartService cart)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _cart = cart;
        }

        public string? GetCurrentPageContext()
        {
            var http = _httpContextAccessor.HttpContext;
            if (http == null) return null;
            var path = http.Request.Path.Value ?? "";
            int? pid = null;
            if (int.TryParse(http.GetRouteData()?.Values["id"]?.ToString(), out var id))
                pid = id;
            return ResolvePageContext(path, pid);
        }

        public string? ResolvePageContext(string? pageUrl, int? productId)
        {
            var path = (pageUrl ?? "").Split('?', '#')[0].ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(path)) return null;

            if (path is "/" or "/home/index" or "/home")
                return "PAGE: Homepage — hero, featured cakes, contact CTA.";

            if (path.Contains("/products/details") || productId.HasValue)
            {
                var pid = productId ?? ExtractIdFromPath(path);
                if (pid.HasValue)
                {
                    var product = _context.Products.AsNoTracking()
                        .Include(p => p.Category)
                        .FirstOrDefault(p => p.ProductId == pid.Value);
                    if (product != null)
                        return $"PAGE: Product Details — khách đang xem **{product.ProductName}** ({product.Price:N0} VND), loại {product.Category?.CategoryName}. Ưu tiên trả lời về món này.";
                }
            }

            if (path.Contains("/products"))
                return "PAGE: Product listing — browse/search/filter cakes.";

            if (path.Contains("/cart/checkout"))
                return "PAGE: Checkout — user must be logged in; collects shipping info then payment.";
            if (path.Contains("/cart/payment"))
                return "PAGE: Payment selection — COD or Stripe Online.";
            if (path.Contains("/cart"))
            {
                var cart = _cart.GetCart();
                return cart.Items.Count > 0
                    ? $"PAGE: Cart — {cart.Items.Count} line(s), total {cart.TotalAmount:N0} VND."
                    : "PAGE: Cart — empty.";
            }

            if (path.Contains("/home/contact"))
                return "PAGE: Contact form — hotline, email, address.";
            if (path.Contains("/admindashboard"))
                return "PAGE: Admin analytics dashboard.";
            if (path.Contains("/identity/account/login"))
                return "PAGE: Login — required before checkout.";

            return $"PAGE: {path}";
        }

        private static int? ExtractIdFromPath(string path)
        {
            var parts = path.Trim('/').Split('/');
            if (parts.Length >= 3 && parts[^2] == "details" && int.TryParse(parts[^1], out var id))
                return id;
            return null;
        }

    }
}
