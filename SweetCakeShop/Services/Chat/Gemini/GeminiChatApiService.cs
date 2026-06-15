using System.Text;
using System.Text.Json;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services.Chat.Gemini
{
    public class GeminiChatApiService : IGeminiChatApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiChatApiService> _logger;

        public GeminiChatApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GeminiChatApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> GenerateReplyAsync(
            string systemInstruction,
            IReadOnlyList<CustomerChatMessage> history,
            string userMessage,
            string factsBlock,
            CancellationToken ct = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey.Trim()}";

            var contents = new List<GeminiContent>();
            foreach (var m in history.TakeLast(6))
            {
                contents.Add(new GeminiContent
                {
                    Role = m.Sender == "model" ? "model" : "user",
                    Parts = [new GeminiPart { Text = m.Content }]
                });
            }

            var userTurn = $"{factsBlock}\n\nCâu hỏi khách: {userMessage}\nTrả lời tối đa 2 câu, đúng dữ liệu.";
            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = [new GeminiPart { Text = userTurn }]
            });

            var payload = new GeminiGenerateRequest
            {
                SystemInstruction = new GeminiContent
                {
                    Role = "user",
                    Parts = [new GeminiPart { Text = systemInstruction }]
                },
                Contents = contents,
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = float.TryParse(_configuration["AiChat:ConsultantTemperature"], out var t) ? t : 0.15f,
                    MaxOutputTokens = int.TryParse(_configuration["AiChat:MaxOutputTokens"], out var n) ? n : 280
                }
            };

            try
            {
                var client = _httpClientFactory.CreateClient("Gemini");
                using var res = await client.PostAsync(url,
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini HTTP {Status}", res.StatusCode);
                    return null;
                }

                var body = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("candidates")[0]
                    .GetProperty("content").GetProperty("parts")[0]
                    .GetProperty("text").GetString()?.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini generate failed");
                return null;
            }
        }
    }
}
