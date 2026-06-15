using SweetCakeShop.Models;

namespace SweetCakeShop.Services.Chat.OpenAi
{
    public interface IOpenAiChatApiService
    {
        Task<string?> GenerateReplyAsync(
            string systemInstruction,
            IReadOnlyList<CustomerChatMessage> history,
            string userMessage,
            string factsBlock,
            CancellationToken ct = default);
    }
}
