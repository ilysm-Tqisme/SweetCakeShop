using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public interface IAiFunctionExecutorService
    {
        Task<AiBusinessContextDto> ExecuteAsync(
            AiChatMode mode,
            AiFunctionPlan plan,
            string userMessage,
            ConversationSessionState sessionState,
            string languageCode,
            CancellationToken cancellationToken = default);
    }
}
