using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public interface IStructuredAiResponseService
    {
        string Compose(AiChatMode mode, AiBusinessContextDto context);
    }
}
