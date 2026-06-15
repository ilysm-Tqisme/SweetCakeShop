using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public interface IConversationMemoryService
    {
        IReadOnlyList<ChatMessage> GetHistory(AiChatMode mode);
        ConversationSessionState GetSessionState(AiChatMode mode);
        void SaveSessionState(AiChatMode mode, ConversationSessionState state);
        void AddExchange(AiChatMode mode, string userMessage, string assistantReply);
        void Clear(AiChatMode mode);
        void UpdateFromContext(AiChatMode mode, AiBusinessContextDto context, string userMessage);
    }
}
