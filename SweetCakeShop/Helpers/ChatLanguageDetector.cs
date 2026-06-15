using System.Text.RegularExpressions;

namespace SweetCakeShop.Helpers
{
    public static class ChatLanguageDetector
    {
        public static string Detect(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "vi";

            var text = message.Trim();

            if (Regex.IsMatch(text, @"[\u3040-\u30ff\u31f0-\u31ff]"))
                return "ja";
            if (Regex.IsMatch(text, @"[\uac00-\ud7af]"))
                return "ko";
            if (Regex.IsMatch(text, @"[\u4e00-\u9fff]") && !Regex.IsMatch(text, @"[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]", RegexOptions.IgnoreCase))
                return "zh";

            var lower = text.ToLowerInvariant();
            var viScore = ScoreVi(lower);
            var enScore = ScoreEn(lower);

            if (Regex.IsMatch(lower, @"\b(le|quel|gâteau|bonjour|merci)\b")) return "fr";
            if (Regex.IsMatch(lower, @"\b(der|die|das|kuchen|welcher|bitte)\b")) return "de";

            return enScore > viScore + 2 ? "en" : "vi";
        }

        public static string LanguageInstruction(string code) => code switch
        {
            "en" => "CRITICAL: Respond ONLY in English. Match the user's language exactly.",
            "fr" => "CRITICAL: Respond ONLY in French. Match the user's language exactly.",
            "de" => "CRITICAL: Respond ONLY in German. Match the user's language exactly.",
            "ja" => "CRITICAL: Respond ONLY in Japanese. Match the user's language exactly.",
            "ko" => "CRITICAL: Respond ONLY in Korean. Match the user's language exactly.",
            "zh" => "CRITICAL: Respond ONLY in Chinese. Match the user's language exactly.",
            _ => "CRITICAL: Respond ONLY in Vietnamese. Match the user's language exactly."
        };

        private static int ScoreVi(string lower)
        {
            var markers = new[] { " bánh", " không", " được", " giá", " giao", " đặt", " như", " nào", " em ", " ạ", " dạ", " shop", " cửa hàng" };
            return markers.Sum(m => lower.Contains(m, StringComparison.Ordinal) ? 1 : 0)
                   + (Regex.IsMatch(lower, @"[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]") ? 3 : 0);
        }

        private static int ScoreEn(string lower)
        {
            var markers = new[] { " the ", " what", " how", " price", " cake", " order", " delivery", " ship", " recommend", " best", " expensive", " revenue", " today" };
            return markers.Sum(m => lower.Contains(m, StringComparison.Ordinal) ? 1 : 0);
        }
    }
}
