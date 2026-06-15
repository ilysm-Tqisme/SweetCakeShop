using System.Text;
using System.Text.Json;
using SweetCakeShop.Models;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public class LlmCompletionService : ILlmCompletionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IPromptBuilderService _promptBuilder;
        private readonly ILogger<LlmCompletionService> _logger;

        public LlmCompletionService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IPromptBuilderService promptBuilder,
            ILogger<LlmCompletionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _promptBuilder = promptBuilder;
            _logger = logger;
        }

        public async Task<string?> TryCompleteAsync(
            AiChatMode mode,
            string systemPrompt,
            string userTurnPrompt,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            var geminiKey = GetApiKey("Gemini:ApiKey", "GEMINI_API_KEY");
            if (geminiKey != null)
            {
                var reply = await CallGeminiAsync(geminiKey, systemPrompt, history, userTurnPrompt, cancellationToken);
                if (!string.IsNullOrWhiteSpace(reply)) return reply;
            }

            var openAiKey = GetApiKey("OpenAI:ApiKey", "OPENAI_API_KEY");
            if (openAiKey != null)
                return await CallOpenAiAsync(openAiKey, systemPrompt, userTurnPrompt, history, cancellationToken);

            return null;
        }

        private string? GetApiKey(string configKey, string envKey)
        {
            var key = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envKey);
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }

        private async Task<string?> CallGeminiAsync(
            string apiKey,
            string systemInstruction,
            IReadOnlyList<ChatMessage> history,
            string userTurnPrompt,
            CancellationToken cancellationToken)
        {
            var models = new[]
            {
                _configuration["Gemini:Model"] ?? "gemini-2.0-flash",
                "gemini-1.5-flash",
                "gemini-2.0-flash-lite"
            };

            foreach (var model in models.Distinct())
            {
                var reply = await TryGeminiModelAsync(apiKey, model, systemInstruction, history, userTurnPrompt, cancellationToken);
                if (!string.IsNullOrWhiteSpace(reply)) return reply;
            }

            return null;
        }

        private async Task<string?> TryGeminiModelAsync(
            string apiKey,
            string model,
            string systemInstruction,
            IReadOnlyList<ChatMessage> history,
            string userTurnPrompt,
            CancellationToken cancellationToken)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var contents = _promptBuilder.BuildGeminiContents(history, userTurnPrompt);
                var payload = new
                {
                    systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
                    contents,
                    generationConfig = new { temperature = 0.85, topP = 0.92, maxOutputTokens = 1200 }
                };

                var client = _httpClientFactory.CreateClient("Gemini");
                using var response = await client.PostAsync(
                    url,
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini {Model} failed {Status}", model, response.StatusCode);
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    return null;

                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var content)
                    && content.TryGetProperty("parts", out var parts)
                    && parts.GetArrayLength() > 0)
                    return parts[0].GetProperty("text").GetString()?.Trim();

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini {Model} error", model);
                return null;
            }
        }

        private async Task<string?> CallOpenAiAsync(
            string apiKey,
            string systemPrompt,
            string userTurnPrompt,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken)
        {
            try
            {
                var endpoint = _configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
                var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
                var client = _httpClientFactory.CreateClient("OpenAI");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var messages = _promptBuilder.BuildOpenAiMessages(systemPrompt, userTurnPrompt, history);
                var payload = new { model, temperature = 0.85, messages };

                using var response = await client.PostAsync(
                    endpoint,
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenAI failed {Status}", response.StatusCode);
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("choices")[0]
                    .GetProperty("message").GetProperty("content").GetString()?.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI error");
                return null;
            }
        }
    }
}
