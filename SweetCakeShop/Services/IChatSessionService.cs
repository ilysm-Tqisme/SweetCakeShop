using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public interface IChatSessionService
    {
        IReadOnlyList<ChatMessage> GetHistory(AiChatMode mode);
        void AddUserMessage(AiChatMode mode, string content);
        void AddAssistantMessage(AiChatMode mode, string content);
        void Clear(AiChatMode mode);
    }
}
