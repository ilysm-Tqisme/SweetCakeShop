using SweetCakeShop.Constants;
using SweetCakeShop.Helpers;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services
{
    public class PromptBuilderService : IPromptBuilderService
    {
        public string BuildSystemPrompt(AiChatMode mode, string languageCode)
        {
            var modeBlock = mode == AiChatMode.Admin ? AiSystemPrompts.AdminMode : AiSystemPrompts.CustomerMode;
            return $"""
                {AiSystemPrompts.CoreArchitecture}

                {modeBlock}

                {ChatLanguageDetector.LanguageInstruction(languageCode)}
                """;
        }

        public string BuildUserTurnPrompt(
            AiChatMode mode,
            string userMessage,
            ChatQueryResult queryResult,
            string languageCode,
            string? pageContext)
        {
            var systemContext = queryResult.ToSystemContextBlock(mode == AiChatMode.Admin);
            var pageLine = string.IsNullOrWhiteSpace(pageContext) ? "" : $"\n{pageContext}";

            return $"""
                User message: {userMessage.Trim()}
                Detected user language: {languageCode}
                {pageLine}

                [System Context]
                Backend executed secure function: {queryResult.Action}
                {systemContext}

                Instructions:
                - Answer using ONLY the system context above for factual data.
                - Respond in the SAME language as the user ({languageCode}).
                - Use conversation history for references (it, that cake, same product).
                - Be natural, premium, and non-repetitive. No robotic templates.
                """;
        }

        public string BuildUserTurnPrompt(
            AiChatMode mode,
            string userMessage,
            AiBusinessContextDto context,
            string languageCode,
            string? pageContext)
        {
            var systemContext = context.ToSystemContextText();
            var pageLine = string.IsNullOrWhiteSpace(pageContext) ? "" : $"\n{pageContext}";

            return $"""
                User message: {userMessage.Trim()}
                Detected user language: {languageCode}
                {pageLine}

                [System Context]
                Pipeline: IntentDetection → BusinessLogic → DatabaseQuery → ContextBuilder
                DetectedIntent: {context.Intent}
                {systemContext}

                Instructions:
                - Answer using ONLY the system context above for prices, revenue, orders, inventory, and product facts.
                - NEVER invent products, prices, or analytics.
                - Respond in the SAME language as the user ({languageCode}).
                - Use conversation history and ConversationFocusProduct for pronouns (it, that cake, món đó).
                - Be natural, warm, concise. No generic spam or repeated templates.
                """;
        }

        public object[] BuildOpenAiMessages(string systemPrompt, string userTurnPrompt, IReadOnlyList<ChatMessage> history)
        {
            var messages = new List<object> { new { role = "system", content = systemPrompt } };

            foreach (var msg in history)
            {
                var role = msg.Role == "assistant" ? "assistant" : "user";
                messages.Add(new { role, content = msg.Content });
            }

            messages.Add(new { role = "user", content = userTurnPrompt });
            return messages.ToArray();
        }

        public List<object> BuildGeminiContents(IReadOnlyList<ChatMessage> history, string userTurnPrompt)
        {
            var contents = new List<object>();
            foreach (var msg in history)
            {
                var role = msg.Role == "assistant" ? "model" : "user";
                contents.Add(new { role, parts = new[] { new { text = msg.Content } } });
            }
            contents.Add(new { role = "user", parts = new[] { new { text = userTurnPrompt } } });
            return contents;
        }
    }
}
