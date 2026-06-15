using System.Text.Json;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public class ChatSessionService : IChatSessionService
    {
        private const int MaxMessages = 20;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatSessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public IReadOnlyList<ChatMessage> GetHistory(AiChatMode mode)
        {
            var session = GetSession();
            if (session == null) return Array.Empty<ChatMessage>();

            var key = GetSessionKey(mode);
            var json = session.GetString(key);
            if (string.IsNullOrEmpty(json)) return Array.Empty<ChatMessage>();

            return JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? [];
        }

        public void AddUserMessage(AiChatMode mode, string content) => AddMessage(mode, "user", content);

        public void AddAssistantMessage(AiChatMode mode, string content) => AddMessage(mode, "assistant", content);

        public void Clear(AiChatMode mode)
        {
            GetSession()?.Remove(GetSessionKey(mode));
        }

        private void AddMessage(AiChatMode mode, string role, string content)
        {
            var session = GetSession();
            if (session == null || string.IsNullOrWhiteSpace(content)) return;

            var list = GetHistory(mode).ToList();
            list.Add(new ChatMessage { Role = role, Content = content.Trim(), Timestamp = DateTime.UtcNow });

            if (list.Count > MaxMessages)
                list = list.Skip(list.Count - MaxMessages).ToList();

            session.SetString(GetSessionKey(mode), JsonSerializer.Serialize(list));
        }

        private static string GetSessionKey(AiChatMode mode) =>
            mode == AiChatMode.Admin ? "SweetCake_AiChat_Admin" : "SweetCake_AiChat_Customer";

        private ISession? GetSession() => _httpContextAccessor.HttpContext?.Session;
    }
}
