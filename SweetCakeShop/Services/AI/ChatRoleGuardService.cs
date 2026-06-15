using SweetCakeShop.Constants;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public class ChatRoleGuardService : IChatRoleGuardService
    {
        private static readonly HashSet<AiIntentType> AdminOnlyIntents =
        [
            AiIntentType.GetTodayRevenue,
            AiIntentType.GetWeeklyRevenue,
            AiIntentType.GetMonthlyRevenue,
            AiIntentType.GetYearlyRevenue,
            AiIntentType.GetTotalOrders,
            AiIntentType.GetAverageOrderValue,
            AiIntentType.GetRevenueSummary,
            AiIntentType.GetWorstSellingProduct,
            AiIntentType.LowInventoryProducts,
            AiIntentType.PendingOrders,
            AiIntentType.OrderStatusSummary,
            AiIntentType.TopCustomers
        ];

        public bool IsAdminOnlyIntent(AiIntentType intent) => AdminOnlyIntents.Contains(intent);

        public AiIntentType RestrictIntentForCustomer(AiIntentType intent) =>
            IsAdminOnlyIntent(intent) ? AiIntentType.ProductConsultation : intent;

        public void SanitizeCustomerContext(AiBusinessContextDto context)
        {
            context.Revenue = null;
            context.Orders = null;
            context.LowInventory.Clear();
            context.Facts.RemoveAll(f =>
                f.Key.Contains("Revenue", StringComparison.OrdinalIgnoreCase) ||
                f.Key.Contains("Order", StringComparison.OrdinalIgnoreCase) ||
                f.Key.Contains("Pending", StringComparison.OrdinalIgnoreCase) ||
                f.Key.Contains("Customer", StringComparison.OrdinalIgnoreCase));
        }

        public string GetAccessDeniedMessage(string languageCode) =>
            languageCode == "en"
                ? "Sorry, that information is only available to store administrators."
                : "Dạ, thông tin này chỉ dành cho quản trị cửa hàng ạ. Anh/chị có thể hỏi về bánh, giá, giao hàng hoặc đặt hàng nhé.";
    }
}
