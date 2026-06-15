using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI.Rag
{
    /// <summary>Chọn ĐÚNG 1 function — tránh GeneralConsultation mặc định gây trả lời sai.</summary>
    public class QueryPlannerService : IQueryPlannerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QueryPlannerService> _logger;

        public QueryPlannerService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<QueryPlannerService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AiFunctionCall> PlanSingleFunctionAsync(
            AiChatMode mode,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState session,
            CancellationToken cancellationToken = default)
        {
            var isAdmin = mode == AiChatMode.Admin;
            var allowed = isAdmin ? AiToolDefinitions.AdminFunctionNames : AiToolDefinitions.CustomerFunctionNames;

            var llmResult = await PlanWithLlmJsonAsync(isAdmin, userMessage, history, session, allowed, cancellationToken);
            if (llmResult != null && IsValidFunction(llmResult.Name, allowed) && llmResult.Name != "GeneralConsultation")
                return llmResult;

            var mapped = SemanticFunctionMapper.Map(userMessage, session, isAdmin);
            if (IsValidFunction(mapped.Name, allowed))
                return mapped;

            return new AiFunctionCall { Name = isAdmin ? "GetRevenueSummary" : "GetProductList", Arguments = new() };
        }

        private async Task<AiFunctionCall?> PlanWithLlmJsonAsync(
            bool isAdmin,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState session,
            string[] allowed,
            CancellationToken ct)
        {
            var key = GetApiKey("OpenAI:ApiKey", "OPENAI_API_KEY")
                      ?? GetApiKey("Gemini:ApiKey", "GEMINI_API_KEY");
            if (key == null) return null;

            var prompt = BuildPlannerPrompt(isAdmin, userMessage, history, session, allowed);

            try
            {
                if (GetApiKey("OpenAI:ApiKey", "OPENAI_API_KEY") != null)
                    return await PlanOpenAiAsync(key, prompt, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI planner failed");
            }

            return null;
        }

        private static string BuildPlannerPrompt(
            bool isAdmin,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState session,
            string[] allowed)
        {
            var sb = new StringBuilder();
            sb.AppendLine(isAdmin ? "Role: Admin staff analytics." : "Role: Customer bakery consultant data lookup.");
            if (session.Focus?.ProductName != null)
                sb.AppendLine($"Focus product: {session.Focus.ProductName}");
            foreach (var m in history.TakeLast(4))
                sb.AppendLine($"{m.Role}: {m.Content}");
            sb.AppendLine($"User question: {userMessage}");
            sb.AppendLine("Allowed functions: " + string.Join(", ", allowed));
            sb.AppendLine("""
                Return JSON ONLY: {"function":"FunctionName","arguments":{}}
                Rules:
                - Pick the ONE best function for the question semantics (any language).
                - cheapest/rẻ nhất -> GetCheapestProduct. expensive/đắt nhất -> GetHighestPriceProduct.
                - best seller/bán chạy -> GetTopSellingProduct. chocolate/socola -> SearchProducts query socola.
                - list cakes/danh sách -> GetProductList. kem -> SearchProducts query kem. mì/bread -> SearchProducts query mì.
                - birthday/sinh nhật/girlfriend -> RecommendProducts with occasion.
                - NEVER use GeneralConsultation if a specific function fits.
                - Admin: revenue today -> GetTodayRevenue; cakes sold today -> GetCakesSoldToday; top customers -> GetTopCustomers.
                """);
            return sb.ToString();
        }

        private async Task<AiFunctionCall?> PlanOpenAiAsync(string apiKey, string prompt, CancellationToken ct)
        {
            var endpoint = _configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model,
                temperature = 0,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = "You classify bakery shop questions into exactly one database function. JSON only." },
                    new { role = "user", content = prompt }
                }
            };

            using var response = await client.PostAsync(
                endpoint,
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return ParseFunctionJson(content);
        }

        private static AiFunctionCall? ParseFunctionJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var name = root.TryGetProperty("function", out var fn) ? fn.GetString() : null;
            if (string.IsNullOrWhiteSpace(name)) return null;

            var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in argsEl.EnumerateObject())
                    args[p.Name] = p.Value.Clone();
            }
            return new AiFunctionCall { Name = name, Arguments = args };
        }

        private static bool IsValidFunction(string name, string[] allowed) =>
            allowed.Contains(name, StringComparer.OrdinalIgnoreCase);

        private string? GetApiKey(string configKey, string envKey)
        {
            var key = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envKey);
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }
    }

    /// <summary>Semantic mapper khi LLM lỗi — dùng ý nghĩa câu, không spam một câu trả lời.</summary>
    internal static class SemanticFunctionMapper
    {
        public static AiFunctionCall Map(string message, ConversationSessionState session, bool isAdmin)
        {
            var t = Regex.Replace(message.ToLowerInvariant().Replace('đ', 'd'), @"\s+", " ");

            if (isAdmin)
            {
                if (Regex.IsMatch(t, @"bán.*(hôm nay|nay)|nay.*ban|cake.*sold|so.*banh.*hom nay"))
                    return new AiFunctionCall { Name = "GetCakesSoldToday", Arguments = new() };
                if (Regex.IsMatch(t, @"doanh thu|revenue|doanh so"))
                {
                    if (Regex.IsMatch(t, @"năm|nam|year")) return Fn("GetYearlyRevenue");
                    if (Regex.IsMatch(t, @"tháng|thang|month")) return Fn("GetMonthlyRevenue");
                    if (Regex.IsMatch(t, @"tuần|tuan|week")) return Fn("GetWeeklyRevenue");
                    return Fn("GetTodayRevenue");
                }
                if (Regex.IsMatch(t, @"bán chạy|ban chay|best sell|top sell")) return Fn("GetTopSellingProduct");
                if (Regex.IsMatch(t, @"khách.*(nhiều|mua)|top customer|vip")) return Fn("GetTopCustomers");
                if (Regex.IsMatch(t, @"nguyên liệu|nguyen lieu|ingredient|tồn kho|ton kho")) return Fn("GetIngredientsOverview");
                return Fn("GetRevenueSummary");
            }

            if (Regex.IsMatch(t, @"rẻ nhất|re nhat|cheapest|lowest price|ít tiền|it tien"))
                return Fn("GetCheapestProduct");
            if (Regex.IsMatch(t, @"đắt nhất|dat nhat|expensive|highest price|cao nhất|cao nhat"))
                return Fn("GetHighestPriceProduct");
            if (Regex.IsMatch(t, @"bán chạy|ban chay|best sell|top sell|phổ biến|pho bien"))
                return Fn("GetTopSellingProduct");
            if (Regex.IsMatch(t, @"socola|chocolate|kem|mì|mi |bread|list|danh sách|danh sach"))
            {
                var q = t.Contains("socola") || t.Contains("chocolate") ? "socola"
                    : t.Contains("kem") ? "kem"
                    : t.Contains("mì") || t.Contains("mi ") || t.Contains("bread") ? "mì"
                    : t.Contains("list") || t.Contains("danh sach") ? "banh" : message;
                return new AiFunctionCall { Name = "SearchProducts", Arguments = new() { ["query"] = q } };
            }
            if (Regex.IsMatch(t, @"sinh nhật|sinh nhat|birthday|bạn gái|ban gai|girlfriend|tặng|tang|quà|qua"))
                return new AiFunctionCall
                {
                    Name = "RecommendProducts",
                    Arguments = new() { ["occasion"] = t.Contains("birthday") || t.Contains("sinh") ? "birthday" : "gift", ["flavor"] = t.Contains("socola") ? "socola" : "" }
                };
            if (Regex.IsMatch(t, @"giá|gia|bao nhiêu|how much") && session.Focus?.ProductName != null)
                return Fn("GetProductDetails");
            if (Regex.IsMatch(t, @"giao hàng|giao hang|delivery|ship")) return Fn("GetDeliveryInformation");
            if (Regex.IsMatch(t, @"thanh toán|thanh toan|payment|cod")) return Fn("GetPaymentInformation");
            if (Regex.IsMatch(t, @"đặt hàng|dat hang|checkout|mua")) return Fn("GetCheckoutGuide");

            return Fn("GetProductList");
        }

        private static AiFunctionCall Fn(string name) => new() { Name = name, Arguments = new() };
    }
}
