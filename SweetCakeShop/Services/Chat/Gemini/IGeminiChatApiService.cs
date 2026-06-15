using SweetCakeShop.Models;

namespace SweetCakeShop.Services.Chat.Gemini
{
    public interface IGeminiChatApiService
    {
        Task<string?> GenerateReplyAsync(
            string systemInstruction,
            IReadOnlyList<CustomerChatMessage> history,
            string userMessage,
            string factsBlock,
            CancellationToken ct = default);
    }
}
