using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI.Rag
{
    public interface IRagRetrieverService
    {
        RagKnowledgeDocument BuildDocument(AiChatMode mode, AiBusinessContextDto context);
    }
}
