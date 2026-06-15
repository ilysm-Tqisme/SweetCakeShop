namespace SweetCakeShop.Services
{
    public interface IChatSecurityService
    {
        bool IsRestrictedRequest(string? message);
        string GetStaffRejectionMessage(string languageCode, AiChatMode mode);
    }
}
