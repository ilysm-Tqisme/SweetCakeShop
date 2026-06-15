using System.Text.Json;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public class ConversationMemoryService : IConversationMemoryService
    {
        private readonly IChatSessionService _session;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConversationMemoryService(IChatSessionService session, IHttpContextAccessor httpContextAccessor)
        {
            _session = session;
            _httpContextAccessor = httpContextAccessor;
        }

        public IReadOnlyList<ChatMessage> GetHistory(AiChatMode mode) => _session.GetHistory(mode);

        public ConversationSessionState GetSessionState(AiChatMode mode)
        {
            var json = GetSession()?.GetString(StateKey(mode));
            return string.IsNullOrEmpty(json)
                ? new ConversationSessionState()
                : JsonSerializer.Deserialize<ConversationSessionState>(json) ?? new ConversationSessionState();
        }

        public void SaveSessionState(AiChatMode mode, ConversationSessionState state)
        {
            GetSession()?.SetString(StateKey(mode), JsonSerializer.Serialize(state));
        }

        public void AddExchange(AiChatMode mode, string userMessage, string assistantReply)
        {
            _session.AddUserMessage(mode, userMessage);
            _session.AddAssistantMessage(mode, assistantReply);

            var state = GetSessionState(mode);
            state.LastAssistantReplySnippet = assistantReply.Length > 200
                ? assistantReply[..200] + "…"
                : assistantReply;
            SaveSessionState(mode, state);
        }

        public void Clear(AiChatMode mode)
        {
            _session.Clear(mode);
            GetSession()?.Remove(StateKey(mode));
        }

        public void UpdateFromContext(AiChatMode mode, AiBusinessContextDto context, string userMessage)
        {
            var state = GetSessionState(mode);

            if (context.Products.Count > 0)
            {
                var p = context.Products[0];
                state.Focus = new ConversationFocus
                {
                    ProductName = p.Name,
                    ProductPrice = p.Price,
                    Category = p.Category
                };
                foreach (var prod in context.Products)
                {
                    if (!state.RecentProductNames.Contains(prod.Name))
                        state.RecentProductNames.Add(prod.Name);
                }
                if (state.RecentProductNames.Count > 10)
                    state.RecentProductNames = state.RecentProductNames.TakeLast(10).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(context.FocusProductName))
            {
                state.Focus ??= new ConversationFocus();
                state.Focus.ProductName = context.FocusProductName;
                state.Focus.ProductPrice = context.FocusProductPrice;
            }

            state.LastDiscussedTopic = !string.IsNullOrWhiteSpace(context.PrimaryExecutedFunction)
                ? context.PrimaryExecutedFunction
                : context.Facts.FirstOrDefault(f => f.Key == "ExecutedFunction")?.Value ?? userMessage;
            state.UserInterest = context.Products.FirstOrDefault()?.Category ?? state.UserInterest;

            SaveSessionState(mode, state);
        }

        private static string StateKey(AiChatMode mode) =>
            mode == AiChatMode.Admin ? "SweetCake_AiSession_Admin" : "SweetCake_AiSession_Customer";

        private ISession? GetSession() => _httpContextAccessor.HttpContext?.Session;
    }
}
