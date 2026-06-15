namespace SweetCakeShop.Services.Chat
{
    public class ChatIdentityService : IChatIdentityService
    {
        public const string ChatTokenCookieName = "ChatToken";
        private readonly IHttpContextAccessor _http;

        public ChatIdentityService(IHttpContextAccessor http) => _http = http;

        public string EnsureChatTokenCookie()
        {
            var ctx = _http.HttpContext ?? throw new InvalidOperationException("No HttpContext");
            if (ctx.User.Identity?.IsAuthenticated == true)
                return string.Empty;

            if (ctx.Request.Cookies.TryGetValue(ChatTokenCookieName, out var existing)
                && !string.IsNullOrWhiteSpace(existing))
                return existing;

            var token = Guid.NewGuid().ToString("N");
            ctx.Response.Cookies.Append(ChatTokenCookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = ctx.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(180),
                IsEssential = true
            });
            return token;
        }

        public (string? UserId, string? ChatToken) GetIdentity()
        {
            var ctx = _http.HttpContext;
            if (ctx == null) return (null, null);

            if (ctx.User.Identity?.IsAuthenticated == true)
            {
                var uid = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return (uid, null);
            }

            ctx.Request.Cookies.TryGetValue(ChatTokenCookieName, out var token);
            return (null, string.IsNullOrWhiteSpace(token) ? null : token);
        }

        public void ClearChatTokenCookie()
        {
            var ctx = _http.HttpContext;
            if (ctx == null) return;
            ctx.Response.Cookies.Delete(ChatTokenCookieName);
        }
    }
}
