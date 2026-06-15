using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public interface IChatEnrichmentService
    {
        string BuildEnrichmentBlock(AiChatMode mode, ConversationSessionState state);
    }
}
