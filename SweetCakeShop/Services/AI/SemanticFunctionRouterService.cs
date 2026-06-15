using System.Text;
using System.Text.Json;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public class SemanticFunctionRouterService : ISemanticFunctionRouterService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IChatEnrichmentService _enrichment;
        private readonly ILogger<SemanticFunctionRouterService> _logger;

        public SemanticFunctionRouterService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IChatEnrichmentService enrichment,
            ILogger<SemanticFunctionRouterService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _enrichment = enrichment;
            _logger = logger;
        }

        public async Task<AiFunctionPlan> PlanFunctionsAsync(
            AiChatMode mode,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState sessionState,
            CancellationToken cancellationToken = default)
        {
            var isAdmin = mode == AiChatMode.Admin;
            var routerPrompt = BuildRouterPrompt(mode, isAdmin, userMessage, history, sessionState);

            var geminiKey = GetApiKey("Gemini:ApiKey", "GEMINI_API_KEY");
            if (geminiKey != null)
            {
                var plan = await PlanWithGeminiAsync(geminiKey, isAdmin, routerPrompt, history, userMessage, cancellationToken);
                if (plan.Calls.Count > 0) return plan;
            }

            var openAiKey = GetApiKey("OpenAI:ApiKey", "OPENAI_API_KEY");
            if (openAiKey != null)
            {
                var plan = await PlanWithOpenAiAsync(openAiKey, isAdmin, routerPrompt, history, userMessage, cancellationToken);
                if (plan.Calls.Count > 0) return plan;
            }

            _logger.LogWarning("Function router: no LLM available, using semantic JSON fallback");
            return await PlanWithJsonFallbackAsync(openAiKey ?? geminiKey, isAdmin, routerPrompt, cancellationToken);
        }

        private string BuildRouterPrompt(
            AiChatMode mode,
            bool isAdmin,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState state)
        {
            var sb = new StringBuilder();
            sb.AppendLine(_enrichment.BuildEnrichmentBlock(mode, state));
            sb.AppendLine(isAdmin ? "ROLE: Admin analytics assistant." : "ROLE: Customer bakery consultant.");
            if (state.Focus != null)
                sb.AppendLine($"CONVERSATION_FOCUS_PRODUCT: {state.Focus.ProductName} | {state.Focus.ProductPrice:N0} VND");
            foreach (var p in state.RecentProductNames.TakeLast(5))
                sb.AppendLine($"RECENT_PRODUCT: {p}");

            sb.AppendLine("RECENT_MESSAGES:");
            foreach (var m in history.TakeLast(10))
                sb.AppendLine($"{m.Role}: {m.Content}");

            sb.AppendLine($"CURRENT_USER_MESSAGE: {userMessage}");
            sb.AppendLine("""
                TASK: Select the correct tool(s) by SEMANTIC meaning (any language). Use tool descriptions.
                Resolve pronouns (it, nó, that cake, cái đó) via CONVERSATION_FOCUS_PRODUCT.
                Combine tools if needed (e.g. RecommendProducts with occasion + flavor from conversation).
                Never pick the same tool for unrelated questions. Never answer the user — only call tools.
                """);
            return sb.ToString();
        }

        private async Task<AiFunctionPlan> PlanWithGeminiAsync(
            string apiKey,
            bool isAdmin,
            string routerPrompt,
            IReadOnlyList<ChatMessage> history,
            string userMessage,
            CancellationToken ct)
        {
            var models = new[] { _configuration["Gemini:Model"] ?? "gemini-2.0-flash", "gemini-1.5-flash" };
            foreach (var model in models.Distinct())
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                    var contents = BuildGeminiHistory(history, routerPrompt);
                    var payload = new
                    {
                        systemInstruction = new
                        {
                            parts = new[] { new { text = "You are a semantic function router. Call tools only — never answer the user directly." } }
                        },
                        contents,
                        tools = AiToolDefinitions.BuildGeminiTools(isAdmin),
                        toolConfig = new { functionCallingConfig = new { mode = "ANY" } },
                        generationConfig = new { temperature = 0.1, maxOutputTokens = 512 }
                    };

                    var client = _httpClientFactory.CreateClient("Gemini");
                    using var response = await client.PostAsync(
                        url,
                        new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
                    var body = await response.Content.ReadAsStringAsync(ct);
                    if (!response.IsSuccessStatusCode) continue;

                    var plan = ParseGeminiFunctionCalls(body);
                    if (plan.Calls.Count > 0) return plan;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gemini router {Model} failed", model);
                }
            }

            return new AiFunctionPlan();
        }

        private static AiFunctionPlan ParseGeminiFunctionCalls(string body)
        {
            var plan = new AiFunctionPlan();
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                return plan;

            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("functionCall", out var fc)) continue;
                var name = fc.GetProperty("name").GetString() ?? "";
                var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (fc.TryGetProperty("args", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in argsEl.EnumerateObject())
                        args[prop.Name] = prop.Value.Clone();
                }
                plan.Calls.Add(new AiFunctionCall { Name = name, Arguments = args });
            }
            return plan;
        }

        private async Task<AiFunctionPlan> PlanWithOpenAiAsync(
            string apiKey,
            bool isAdmin,
            string routerPrompt,
            IReadOnlyList<ChatMessage> history,
            string userMessage,
            CancellationToken ct)
        {
            try
            {
                var endpoint = _configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
                var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
                var messages = new List<object>
                {
                    new { role = "system", content = "Semantic function router for SweetCakeShop. Select tools by meaning, not keywords. Support all languages." },
                    new { role = "user", content = routerPrompt }
                };

                var client = _httpClientFactory.CreateClient("OpenAI");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var payload = new
                {
                    model,
                    temperature = 0.1,
                    messages,
                    tools = AiToolDefinitions.BuildOpenAiTools(isAdmin),
                    tool_choice = "auto"
                };

                using var response = await client.PostAsync(
                    endpoint,
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode) return new AiFunctionPlan();

                return ParseOpenAiToolCalls(body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI router failed");
                return new AiFunctionPlan();
            }
        }

        private static AiFunctionPlan ParseOpenAiToolCalls(string body)
        {
            var plan = new AiFunctionPlan();
            using var doc = JsonDocument.Parse(body);
            var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
            if (!message.TryGetProperty("tool_calls", out var toolCalls)) return plan;

            foreach (var tc in toolCalls.EnumerateArray())
            {
                var fn = tc.GetProperty("function");
                var name = fn.GetProperty("name").GetString() ?? "";
                var argsJson = fn.GetProperty("arguments").GetString() ?? "{}";
                var args = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson)
                           ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                plan.Calls.Add(new AiFunctionCall { Name = name, Arguments = args });
            }
            return plan;
        }

        private async Task<AiFunctionPlan> PlanWithJsonFallbackAsync(
            string? apiKey,
            bool isAdmin,
            string routerPrompt,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(apiKey)) return DefaultPlanFromHeuristics(routerPrompt, isAdmin);

            try
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
                        new
                        {
                            role = "system",
                            content = "Return JSON only: {\"functions\":[{\"name\":\"FunctionName\",\"arguments\":{}}]}. "
                                + "Pick function by semantic meaning. Allowed names: "
                                + string.Join(", ", isAdmin ? AiToolDefinitions.AdminFunctionNames : AiToolDefinitions.CustomerFunctionNames)
                        },
                        new { role = "user", content = routerPrompt }
                    }
                };

                using var response = await client.PostAsync(
                    endpoint,
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode) return DefaultPlanFromHeuristics(routerPrompt, isAdmin);

                using var doc = JsonDocument.Parse(body);
                var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                if (string.IsNullOrWhiteSpace(content)) return DefaultPlanFromHeuristics(routerPrompt, isAdmin);

                using var json = JsonDocument.Parse(content);
                var plan = new AiFunctionPlan();
                if (json.RootElement.TryGetProperty("functions", out var arr))
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        var name = item.GetProperty("name").GetString() ?? "";
                        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        if (item.TryGetProperty("arguments", out var argsEl))
                        {
                            foreach (var p in argsEl.EnumerateObject())
                                args[p.Name] = p.Value.Clone();
                        }
                        if (!string.IsNullOrEmpty(name))
                            plan.Calls.Add(new AiFunctionCall { Name = name, Arguments = args });
                    }
                }
                return plan.Calls.Count > 0 ? plan : DefaultPlanFromHeuristics(routerPrompt, isAdmin);
            }
            catch
            {
                return DefaultPlanFromHeuristics(routerPrompt, isAdmin);
            }
        }

        private static AiFunctionPlan DefaultPlanFromHeuristics(string routerPrompt, bool isAdmin)
        {
            _ = routerPrompt;
            return new AiFunctionPlan
            {
                Calls = [new AiFunctionCall { Name = "GeneralConsultation", Arguments = new() }]
            };
        }

        private static List<object> BuildGeminiHistory(IReadOnlyList<ChatMessage> history, string userMessage)
        {
            var list = new List<object>();
            foreach (var m in history.TakeLast(6))
            {
                var role = m.Role == "assistant" ? "model" : "user";
                list.Add(new { role, parts = new[] { new { text = m.Content } } });
            }
            list.Add(new { role = "user", parts = new[] { new { text = userMessage } } });
            return list;
        }

        private string? GetApiKey(string configKey, string envKey)
        {
            var key = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envKey);
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }
    }
}
