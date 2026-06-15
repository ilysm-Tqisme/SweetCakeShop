namespace SweetCakeShop.Services.Chat
{
    public class ChatTokenMergeService : IChatTokenMergeService
    {
        private readonly IHttpContextAccessor _http;
        private readonly IChatHistoryService _history;
        private readonly IChatIdentityService _identity;

        public ChatTokenMergeService(
            IHttpContextAccessor http,
            IChatHistoryService history,
            IChatIdentityService identity)
        {
            _http = http;
            _history = history;
            _identity = identity;
        }

        public async Task TryMergeOnAuthenticatedRequestAsync(CancellationToken ct = default)
        {
            var ctx = _http.HttpContext;
            if (ctx?.User.Identity?.IsAuthenticated != true) return;

            var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            if (!ctx.Request.Cookies.TryGetValue(ChatIdentityService.ChatTokenCookieName, out var token)
                || string.IsNullOrWhiteSpace(token))
                return;

            await _history.MergeChatTokenToUserAsync(token, userId, ct);
            _identity.ClearChatTokenCookie();
        }

        public async Task MergeOnLoginAsync(string userId, CancellationToken ct = default)
        {
            var ctx = _http.HttpContext;
            if (ctx == null) return;

            if (!ctx.Request.Cookies.TryGetValue(ChatIdentityService.ChatTokenCookieName, out var token)
                || string.IsNullOrWhiteSpace(token))
                return;

            await _history.MergeChatTokenToUserAsync(token, userId, ct);
            _identity.ClearChatTokenCookie();
        }
    }
}
