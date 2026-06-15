using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;
using SweetCakeShop.Services.AI;

namespace SweetCakeShop.Services.AI.Rag
{
    public interface IQueryPlannerService
    {
        Task<AiFunctionCall> PlanSingleFunctionAsync(
            AiChatMode mode,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState session,
            CancellationToken cancellationToken = default);
    }
}
