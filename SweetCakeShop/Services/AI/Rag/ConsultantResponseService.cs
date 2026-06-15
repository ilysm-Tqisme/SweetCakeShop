using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI.Rag
{
    public class ConsultantResponseService : IConsultantResponseService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConsultantResponseService> _logger;

        public ConsultantResponseService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ConsultantResponseService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> GenerateAsync(
            AiChatMode mode,
            string userMessage,
            RagKnowledgeDocument knowledge,
            IReadOnlyList<ChatMessage> history,
            string languageCode,
            ConversationSessionState session,
            CancellationToken cancellationToken = default)
        {
            var system = BuildConsultantSystemPrompt(mode);
            var user = BuildUserPrompt(userMessage, knowledge, session, languageCode);

            var geminiKey = GetApiKey("Gemini:ApiKey", "GEMINI_API_KEY");
            if (geminiKey != null)
            {
                var r = await TryGeminiAsync(geminiKey, system, history, user, cancellationToken);
                if (!string.IsNullOrWhiteSpace(r)) return TrimToMaxSentences(r!, 3);
            }

            var openAiKey = GetApiKey("OpenAI:ApiKey", "OPENAI_API_KEY");
            if (openAiKey != null)
            {
                var r = await TryOpenAiAsync(openAiKey, system, history, user, cancellationToken);
                if (!string.IsNullOrWhiteSpace(r)) return TrimToMaxSentences(r!, 3);
            }

            return null;
        }

        private static string BuildConsultantSystemPrompt(AiChatMode mode)
        {
            if (mode == AiChatMode.Admin)
            {
                return """
                    Bạn là nhân viên phân tích nội bộ SweetCakeShop — thân thiện, chuyên nghiệp, NGẮN GỌN.
                    CHỈ trả lời dựa trên [Dữ liệu cửa hàng]. Không bịa số liệu.
                    Trả lời đúng câu hỏi (doanh thu, bánh bán, khách VIP, nguyên liệu...). Tối đa 3 câu. Có thể dùng 📊.
                    """;
            }

            return """
                Bạn là nhân viên tư vấn bán bánh vô cùng thân thiện, chuyên nghiệp và ngắn gọn của cửa hàng SweetCakeShop.

                NHIỆM VỤ:
                1. Tư vấn bánh phù hợp (sở thích, số người, ngân sách) theo DỮ LIỆU.
                2. Khi khách muốn đặt: gợi ý thu thập Tên, SĐT, Địa chỉ, Ngày giờ nhận, Nội dung ghi bánh (hướng dẫn ngắn).

                QUY TẮC BẮT BUỘC:
                - CHỈ trả lời từ [Dữ liệu cửa hàng]. KHÔNG bịa bánh, giá, chính sách.
                - Không có dữ liệu: "Dạ hiện tại tiệm em chưa có loại bánh này/chưa có thông tin này, anh/chị đợi em một chút để em báo nhân viên tiệm hỗ trợ mình ngay nhé ạ!" — không nói "Tôi không biết".
                - NGẮN GỌN: tối đa 3 câu. Không spam, không lặp, không dài dòng.
                - Dùng icon 🍰 🎂 🍩 phù hợp (1-2 cái).
                - Xưng hô: Dạ, anh/chị, em.
                """;
        }

        private static string BuildUserPrompt(
            string userMessage,
            RagKnowledgeDocument knowledge,
            ConversationSessionState session,
            string languageCode)
        {
            var focus = session.Focus?.ProductName != null
                ? $"\nSản phẩm đang nói: {session.Focus.ProductName}"
                : "";
            return $"""
                Ngôn ngữ trả lời: {languageCode}
                Loại truy vấn dữ liệu: {knowledge.PrimaryFunction}
                {focus}

                [Dữ liệu cửa hàng]
                {knowledge.StoreDataBlock}

                Câu hỏi khách: {userMessage}

                Trả lời ĐÚNG câu hỏi, tối đa 3 câu.
                """;
        }

        private static string TrimToMaxSentences(string text, int max)
        {
            var cleaned = text.Trim();
            var parts = Regex.Split(cleaned, @"(?<=[.!?…])\s+");
            if (parts.Length <= max) return cleaned;
            return string.Join(" ", parts.Take(max));
        }

        private async Task<string?> TryGeminiAsync(
            string apiKey,
            string system,
            IReadOnlyList<ChatMessage> history,
            string userPrompt,
            CancellationToken ct)
        {
            try
            {
                var model = _configuration["Gemini:Model"] ?? "gemini-2.0-flash";
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var contents = new List<object>();
                foreach (var m in history.TakeLast(4))
                {
                    var role = m.Role == "assistant" ? "model" : "user";
                    contents.Add(new { role, parts = new[] { new { text = m.Content } } });
                }
                contents.Add(new { role = "user", parts = new[] { new { text = userPrompt } } });

                var payload = new
                {
                    systemInstruction = new { parts = new[] { new { text = system } } },
                    contents,
                    generationConfig = new { temperature = GetConsultantTemperature(), maxOutputTokens = GetMaxTokens() }
                };

                var client = _httpClientFactory.CreateClient("Gemini");
                using var res = await client.PostAsync(url,
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
                if (!res.IsSuccessStatusCode) return null;

                var body = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("candidates")[0]
                    .GetProperty("content").GetProperty("parts")[0]
                    .GetProperty("text").GetString()?.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini consultant failed");
                return null;
            }
        }

        private async Task<string?> TryOpenAiAsync(
            string apiKey,
            string system,
            IReadOnlyList<ChatMessage> history,
            string userPrompt,
            CancellationToken ct)
        {
            try
            {
                var messages = new List<object> { new { role = "system", content = system } };
                foreach (var m in history.TakeLast(4))
                    messages.Add(new { role = m.Role == "assistant" ? "assistant" : "user", content = m.Content });
                messages.Add(new { role = "user", content = userPrompt });

                var client = _httpClientFactory.CreateClient("OpenAI");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var payload = new
                {
                    model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini",
                    temperature = GetConsultantTemperature(),
                    max_tokens = GetMaxTokens(),
                    messages
                };

                using var res = await client.PostAsync(
                    _configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
                if (!res.IsSuccessStatusCode) return null;

                var body = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("choices")[0]
                    .GetProperty("message").GetProperty("content").GetString()?.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI consultant failed");
                return null;
            }
        }

        private string? GetApiKey(string configKey, string envKey)
        {
            var key = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envKey);
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }

        /// <summary>Thấp (0.1–0.2) = ít bịa, ít spam. Mặc định 0.15.</summary>
        private float GetConsultantTemperature()
        {
            var v = _configuration["AiChat:ConsultantTemperature"];
            return float.TryParse(v, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var t)
                ? Math.Clamp(t, 0f, 0.4f)
                : 0.15f;
        }

        private int GetMaxTokens()
        {
            var v = _configuration["AiChat:MaxOutputTokens"];
            return int.TryParse(v, out var n) ? Math.Clamp(n, 80, 500) : 280;
        }
    }
}
