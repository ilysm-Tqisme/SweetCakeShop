using System.Text;
using System.Text.RegularExpressions;
using SweetCakeShop.Constants;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services.AI
{
    public class IntentDetectionService : IIntentDetectionService
    {
        private static readonly (AiIntentType Intent, string[] Phrases)[] CustomerIntents =
        [
            (AiIntentType.GetHighestPriceProduct, ["đắt nhất", "dat nhat", "cao nhất", "most expensive", "highest price", "max price", "giá cao"]),
            (AiIntentType.GetLowestPriceProduct, ["rẻ nhất", "re nhat", "cheapest", "lowest price", "giá thấp"]),
            (AiIntentType.GetTopSellingProduct, ["bán chạy", "ban chay", "best seller", "top cake", "top selling", "most purchased", "phổ biến", "yêu thích"]),
            (AiIntentType.DeliveryQuestion, ["giao hàng", "giao hang", "ship", "delivery", "bao lâu nhận", "how long", "vận chuyển", "do you ship"]),
            (AiIntentType.PaymentQuestion, ["thanh toán", "thanh toan", "payment", "cod", "stripe", "online pay", "trả tiền"]),
            (AiIntentType.PromotionQuestion, ["khuyến mãi", "khuyen mai", "promotion", "giảm giá", "discount", "sale", "voucher"]),
            (AiIntentType.OrderGuide, ["đặt hàng", "dat hang", "how to order", "cách mua", "checkout", "giỏ hàng", "cart"]),
            (AiIntentType.CartAssistance, ["giỏ", "cart", "trong giỏ"]),
            (AiIntentType.RecommendProduct, ["gợi ý", "goi y", "recommend", "sinh nhật", "birthday", "girlfriend", "boyfriend", "tiệc", "party", "chocolate", "ít ngọt", "under", "dưới"]),
            (AiIntentType.ContactInfo, ["liên hệ", "lien he", "hotline", "contact", "email", "gọi"]),
            (AiIntentType.ProductPriceLookup, ["giá bao nhiêu", "how much", "price of", "bao nhiêu tiền", "cost"]),
            (AiIntentType.ProductTasteConsultation, ["ngon nhất", "ngon nhat", "ngon không", "best taste", "delicious", "nên mua", "gì ngon", "banh nao ngon"]),
            (AiIntentType.AddToCart, ["lấy", "lay", "cho chị", "cho anh", "thêm vào giỏ", "add to cart", "mua giúp"])
        ];

        private static readonly (AiIntentType Intent, string[] Phrases)[] AdminIntents =
        [
            (AiIntentType.GetTodayRevenue, ["hôm nay", "hom nay", "today revenue", "today sales", "doanh thu hôm nay", "bán được bao nhiêu"]),
            (AiIntentType.GetWeeklyRevenue, ["tuần", "tuan", "week", "7 ngày", "weekly"]),
            (AiIntentType.GetMonthlyRevenue, ["tháng", "thang", "month", "monthly"]),
            (AiIntentType.GetYearlyRevenue, ["năm", "nam", "year", "yearly"]),
            (AiIntentType.GetTotalOrders, ["tổng đơn", "tong don", "total order", "bao nhiêu đơn"]),
            (AiIntentType.GetAverageOrderValue, ["trung bình", "average order", "tb đơn", "aov"]),
            (AiIntentType.GetTopSellingProduct, ["bán chạy", "top", "best seller", "ban chay"]),
            (AiIntentType.GetWorstSellingProduct, ["bán kém", "ban kem", "worst", "low selling"]),
            (AiIntentType.PendingOrders, ["pending", "chờ", "cho xu ly", "đơn chờ"]),
            (AiIntentType.LowInventoryProducts, ["tồn kho", "ton kho", "inventory", "sắp hết", "low stock", "nguyên liệu"]),
            (AiIntentType.OrderStatusSummary, ["trạng thái", "status", "phân bổ đơn"]),
            (AiIntentType.GetRevenueSummary, ["doanh thu", "revenue", "doanh số", "tổng quan", "summary", "analytics"]),
            (AiIntentType.TopCustomers, ["khách mua nhiều", "khach mua nhieu", "top customer", "vip", "khách hàng thân thiết"])
        ];

        public AiIntentType Detect(AiChatMode mode, string userMessage, IReadOnlyList<ChatMessage> history)
        {
            var text = BuildText(userMessage, history);

            if (IsPronounPriceFollowUp(text, history))
                return AiIntentType.ProductPriceLookup;

            if (mode == AiChatMode.Customer && ChatIntentHelper.LooksLikeAddToCart(userMessage) && history.Count > 0)
                return AiIntentType.AddToCart;

            var profiles = mode == AiChatMode.Admin ? AdminIntents : CustomerIntents;
            var best = AiIntentType.General;
            var bestScore = 0.0;

            foreach (var (intent, phrases) in profiles)
            {
                var score = Score(text, phrases);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = intent;
                }
            }

            return bestScore >= 0.35 ? best : AiIntentType.General;
        }

        private static bool IsPronounPriceFollowUp(string text, IReadOnlyList<ChatMessage> history)
        {
            if (history.Count == 0) return false;
            var pronouns = new[] { "giá", "gia", "bao nhiêu", "how much", "price", "nó", "no", "it", "món đó", "that one", "cái đó" };
            return pronouns.Any(p => text.Contains(p, StringComparison.Ordinal));
        }

        private static string BuildText(string message, IReadOnlyList<ChatMessage> history)
        {
            var sb = new StringBuilder(Normalize(message));
            foreach (var m in history.TakeLast(4))
                sb.Append(' ').Append(Normalize(m.Content));
            return sb.ToString();
        }

        private static double Score(string text, string[] phrases)
        {
            double max = 0;
            var tokens = Tokenize(text);
            foreach (var phrase in phrases)
            {
                var n = Normalize(phrase);
                if (text.Contains(n, StringComparison.Ordinal)) { max = Math.Max(max, 1); continue; }
                var pt = Tokenize(n);
                if (pt.Length > 0)
                    max = Math.Max(max, (double)pt.Count(t => tokens.Contains(t)) / pt.Length);
            }
            return max;
        }

        private static string[] Tokenize(string t) =>
            Regex.Split(t, @"\W+").Where(x => x.Length > 1).Distinct().ToArray();

        private static string Normalize(string s)
        {
            var l = s.Trim().ToLowerInvariant().Replace('đ', 'd');
            return Regex.Replace(l, @"\s+", " ");
        }
    }
}
