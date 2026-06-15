using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;
using SweetCakeShop.Services.AI;

namespace SweetCakeShop.Services.AI.Rag
{
    public interface IConsultantResponseService
    {
        Task<string?> GenerateAsync(
            AiChatMode mode,
            string userMessage,
            RagKnowledgeDocument knowledge,
            IReadOnlyList<ChatMessage> history,
            string languageCode,
            ConversationSessionState session,
            CancellationToken cancellationToken = default);
    }
}
