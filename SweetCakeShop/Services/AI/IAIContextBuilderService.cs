using SweetCakeShop.Constants;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public interface IAIContextBuilderService
    {
        Task<AiBusinessContextDto> BuildAsync(
            AiChatMode mode,
            AiIntentType intent,
            string userMessage,
            string languageCode,
            IReadOnlyList<ChatMessage> history,
            string? pageContext,
            CancellationToken cancellationToken = default);
    }
}
