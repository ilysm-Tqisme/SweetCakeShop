using System.Text.RegularExpressions;

namespace SweetCakeShop.Services
{
    public class TopicFilterService : ITopicFilterService
    {
        private static readonly string[] BlockedTopics =
        [
            "chính trị", "politics", "election", "tôn giáo", "religion", "chiến tranh", "war",
            "hack", "hacking", "malware", "exploit", "illegal",
            "thời tiết", "weather forecast", "dự báo thời tiết",
            "trò chơi", "game online", "gaming",
            "lập trình python", "viết code", "sql injection"
        ];

        public bool IsClearlyOffTopic(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return true;
            var normalized = Regex.Replace(message.Trim().ToLowerInvariant(), @"\s+", " ");
            return BlockedTopics.Any(t => normalized.Contains(t, StringComparison.Ordinal));
        }

        public string GetRejectionMessage(string languageCode) => languageCode switch
        {
            "en" => "Sorry, I can only assist with SweetCakeShop-related topics and services.",
            "ja" => "申し訳ございません。SweetCakeShopに関するご質問のみお答えできます。",
            "ko" => "죄송합니다. SweetCakeShop 관련 문의만 도와드릴 수 있습니다.",
            "zh" => "抱歉，我只能协助与 SweetCakeShop 相关的问题。",
            _ => "Dạ, em là trợ lý của SweetCakeShop nên chỉ có thể tư vấn cho anh/chị các thông tin về bánh và dịch vụ của cửa hàng thôi ạ."
        };
    }
}
