using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    /// <summary>Uses Gemini/OpenAI tool calling for semantic routing — NOT keyword matching.</summary>
    public interface ISemanticFunctionRouterService
    {
        Task<AiFunctionPlan> PlanFunctionsAsync(
            AiChatMode mode,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState sessionState,
            CancellationToken cancellationToken = default);
    }
}
