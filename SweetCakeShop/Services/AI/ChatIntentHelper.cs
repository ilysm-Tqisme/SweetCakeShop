using System.Text.RegularExpressions;

namespace SweetCakeShop.Services.AI
{
    public static class ChatIntentHelper
    {
        public static int ExtractTopCount(string message, int defaultCount = 5)
        {
            var text = message.ToLowerInvariant();
            var m = Regex.Match(text, @"top\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n is > 0 and <= 20)
                return n;

            m = Regex.Match(text, @"(\d+)\s*(?:banh|bánh|món|san pham|sản phẩm|cake)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out n) && n is > 0 and <= 20)
                return n;

            if (text.Contains("top 10") || text.Contains("10 món")) return 10;
            if (text.Contains("top 5") || text.Contains("5 món")) return 5;
            return defaultCount;
        }

        public static int ExtractQuantity(string message, int defaultQty = 1)
        {
            var m = Regex.Match(message, @"(\d+)\s*(?:cái|cai|chiếc|chiec|hộp|hop|phần|phan|piece|pcs)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var q) && q is > 0 and <= 99)
                return q;

            m = Regex.Match(message, @"(?:lấy|lay|cho|mua|add)\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out q) && q is > 0 and <= 99)
                return q;

            return defaultQty;
        }

        public static bool LooksLikeAddToCart(string message)
        {
            var t = message.ToLowerInvariant();
            return t.Contains("lấy") || t.Contains("lay") || t.Contains("cho chị") || t.Contains("cho anh")
                   || t.Contains("mua") || t.Contains("thêm vào giỏ") || t.Contains("add to cart")
                   || Regex.IsMatch(t, @"\d+\s*(cái|cai|chiếc)");
        }
    }
}
