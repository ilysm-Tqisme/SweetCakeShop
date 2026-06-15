using SweetCakeShop.Models.Api;

namespace SweetCakeShop.Services
{
    public interface IAiChatService
    {
        Task<string> GetAdminReplyAsync(string userMessage, CancellationToken cancellationToken = default);
        Task<ChatReplyResult> GetCustomerReplyAsync(
            string userMessage,
            ChatCustomerContext? clientContext = null,
            CancellationToken cancellationToken = default);
    }
}
