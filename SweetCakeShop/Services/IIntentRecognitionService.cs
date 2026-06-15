using SweetCakeShop.Constants;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public interface IIntentRecognitionService
    {
        IReadOnlyList<ChatIntent> DetectIntents(
            AiChatMode mode,
            string userMessage,
            IReadOnlyList<ChatMessage> conversationHistory);

        ChatQueryAction DetectQueryAction(
            AiChatMode mode,
            string userMessage,
            IReadOnlyList<ChatMessage> conversationHistory);
    }
}
