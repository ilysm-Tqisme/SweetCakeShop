using SweetCakeShop.Constants;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    /// <summary>Enforces role-based data access — customers never receive admin/business context.</summary>
    public interface IChatRoleGuardService
    {
        AiIntentType RestrictIntentForCustomer(AiIntentType intent);
        bool IsAdminOnlyIntent(AiIntentType intent);
        void SanitizeCustomerContext(AiBusinessContextDto context);
        string GetAccessDeniedMessage(string languageCode);
    }
}
