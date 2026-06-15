using SweetCakeShop.Models;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public interface ILlmCompletionService
    {
        Task<string?> TryCompleteAsync(
            AiChatMode mode,
            string systemPrompt,
            string userTurnPrompt,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default);
    }
}
