using SweetCakeShop.Constants;
using SweetCakeShop.Models;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public interface IIntentDetectionService
    {
        AiIntentType Detect(AiChatMode mode, string userMessage, IReadOnlyList<ChatMessage> history);
    }
}
