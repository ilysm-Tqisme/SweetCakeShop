using System.Text;
using System.Text.Json;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services.Chat.OpenAi
{
    public class OpenAiChatApiService : IOpenAiChatApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public OpenAiChatApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string?> GenerateReplyAsync(
            string systemInstruction,
            IReadOnlyList<CustomerChatMessage> history,
            string userMessage,
            string factsBlock,
            CancellationToken ct = default)
        {
            var apiKey = _configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            var messages = new List<object> { new { role = "system", content = systemInstruction } };
            foreach (var m in history.TakeLast(6))
                messages.Add(new { role = m.Sender == "model" ? "assistant" : "user", content = m.Content });
            messages.Add(new
            {
                role = "user",
                content = $"{factsBlock}\n\nCâu hỏi: {userMessage}\nTrả lời tối đa 2 câu."
            });

            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey.Trim());

            var payload = new
            {
                model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini",
                temperature = 0.15,
                max_tokens = 280,
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
    }
}
