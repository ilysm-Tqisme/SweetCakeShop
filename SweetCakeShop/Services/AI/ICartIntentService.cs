using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public interface ICartIntentService
    {
        Task<bool> TryAddFocusedProductAsync(string userMessage, ConversationFocus? focus, AiBusinessContextDto context, CancellationToken ct = default);
    }
}
