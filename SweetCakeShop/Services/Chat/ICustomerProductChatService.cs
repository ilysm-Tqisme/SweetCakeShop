using SweetCakeShop.Models.Api;

namespace SweetCakeShop.Services.Chat
{
    public interface ICustomerProductChatService
    {
        Task<ChatHistoryResponse> GetChatHistoryAsync(CancellationToken ct = default);
        Task<SendChatMessageResponse> SendMessageAsync(SendChatMessageRequest request, CancellationToken ct = default);
    }
}
