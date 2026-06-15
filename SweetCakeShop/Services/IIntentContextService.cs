using SweetCakeShop.Constants;

namespace SweetCakeShop.Services
{
    public interface IIntentContextService
    {
        Task<string> BuildContextAsync(
            AiChatMode mode,
            IReadOnlyList<ChatIntent> intents,
            string userMessage,
            CancellationToken cancellationToken = default);
    }
}
