using System.Text;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public class ChatEnrichmentService : IChatEnrichmentService
    {
        private readonly IWebsiteKnowledgeService _website;
        private readonly IChatPageContextService _pageContext;
        private readonly CartService _cart;

        public ChatEnrichmentService(
            IWebsiteKnowledgeService website,
            IChatPageContextService pageContext,
            CartService cart)
        {
            _website = website;
            _pageContext = pageContext;
            _cart = cart;
        }

        public string BuildEnrichmentBlock(AiChatMode mode, ConversationSessionState state)
        {
            var sb = new StringBuilder();
            sb.AppendLine(mode == AiChatMode.Admin
                ? _website.GetAdminCapabilities()
                : _website.GetCustomerCapabilities());

            var page = _pageContext.GetCurrentPageContext();
            if (!string.IsNullOrWhiteSpace(page))
                sb.AppendLine(page);

            if (mode == AiChatMode.Customer)
            {
                var cart = _cart.GetCart();
                if (cart.Items.Count > 0)
                {
                    sb.AppendLine("CURRENT_CART:");
                    foreach (var i in cart.Items)
                        sb.AppendLine($"- {i.ProductName} x{i.Quantity} = {i.Subtotal:N0} VND");
                    sb.AppendLine($"CART_TOTAL: {cart.TotalAmount:N0} VND");
                }
            }

            if (state.Preferences.Occasion != null)
                sb.AppendLine($"USER_PREFERENCE_OCCASION: {state.Preferences.Occasion}");
            if (state.Preferences.FlavorOrStyle != null)
                sb.AppendLine($"USER_PREFERENCE_FLAVOR: {state.Preferences.FlavorOrStyle}");
            if (state.Preferences.BudgetMax.HasValue)
                sb.AppendLine($"USER_PREFERENCE_BUDGET: {state.Preferences.BudgetMax:N0} VND");

            return sb.ToString();
        }
    }
}
