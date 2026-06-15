using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Formats structured DB query results into natural Vietnamese when LLM is unavailable.
    /// Not keyword-based — driven only by query result payload.
    /// </summary>
    public interface IAiResponseComposer
    {
        string Compose(AiChatMode mode, ChatQueryResult queryResult, string languageCode);
    }
}
