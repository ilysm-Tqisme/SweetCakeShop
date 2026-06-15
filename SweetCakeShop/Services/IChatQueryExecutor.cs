using SweetCakeShop.Constants;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public interface IChatQueryExecutor
    {
        Task<ChatQueryResult> ExecuteAsync(
            AiChatMode mode,
            ChatQueryAction action,
            string userMessage,
            CancellationToken cancellationToken = default);
    }
}
