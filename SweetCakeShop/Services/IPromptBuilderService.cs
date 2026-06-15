using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services
{
    public enum AiChatMode
    {
        Admin,
        Customer
    }

    public interface IPromptBuilderService
    {
        string BuildSystemPrompt(AiChatMode mode, string languageCode);
        string BuildUserTurnPrompt(AiChatMode mode, string userMessage, ChatQueryResult queryResult, string languageCode, string? pageContext);
        string BuildUserTurnPrompt(AiChatMode mode, string userMessage, AiBusinessContextDto context, string languageCode, string? pageContext);
        object[] BuildOpenAiMessages(string systemPrompt, string userTurnPrompt, IReadOnlyList<ChatMessage> history);
        List<object> BuildGeminiContents(IReadOnlyList<ChatMessage> history, string userTurnPrompt);
    }
}
