using System.Text;
using System.Text.RegularExpressions;
using SweetCakeShop.Constants;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Semantic intent scoring via synonym/phrase profiles and conversation context — not hardcoded replies.
    /// </summary>
    public class IntentRecognitionService : IIntentRecognitionService
    {
        private static readonly Dictionary<ChatIntent, string[]> CustomerProfiles = new()
        {
            [ChatIntent.Delivery] =
            [
                "giao hàng", "giao hang", "ship", "shipping", "delivery", "nhận bánh", "nhan banh",
                "bao lâu", "bao lau", "mấy ngày", "may ngay", "tận nơi", "tan noi", "nội thành", "noi thanh",
                "vận chuyển", "van chuyen", "nhận hàng", "khi nào nhận"
            ],
            [ChatIntent.Pricing] =
            [
                "giá", "gia", "price", "bao nhiêu tiền", "bao nhieu tien", "cost", "chi phí",
                "dưới", "duoi", "trên", "tren", "rẻ", "re", "đắt", "dat", "k", "triệu", "trieu"
            ],
            [ChatIntent.ProductRecommendation] =
            [
                "gợi ý", "goi y", "recommend", "nên mua", "nen mua", "sinh nhật", "sinh nhat",
                "tiệc", "tiec", "bé gái", "be gai", "ít ngọt", "it ngot", "chocolate", "socola",
                "mousse", "tiramisu", "bánh kem", "banh kem", "bán chạy", "ban chay", "ngon",
                "cặp đôi", "cap doi", "công ty", "cong ty", "phù hợp", "phu hop"
            ],
            [ChatIntent.OrderSupport] =
            [
                "đặt hàng", "dat hang", "mua", "thanh toán", "thanh toan", "cod", "online",
                "giỏ hàng", "gio hang", "checkout", "đơn hàng", "don hang", "cách mua"
            ],
            [ChatIntent.Promotions] =
            [
                "giảm giá", "giam gia", "khuyến mãi", "khuyen mai", "promotion", "ưu đãi", "uu dai",
                "sale", "voucher", "mã giảm"
            ],
            [ChatIntent.Contact] =
            [
                "liên hệ", "lien he", "hotline", "email", "địa chỉ", "dia chi", "contact", "gọi"
            ]
        };

        private static readonly Dictionary<ChatIntent, string[]> AdminProfiles = new()
        {
            [ChatIntent.RevenueAnalytics] =
            [
                "doanh thu", "doanh thu hom nay", "revenue", "doanh số", "doanh so", "kiếm được",
                "bán được bao nhiêu", "tổng tiền", "tong tien", "hôm nay", "hom nay", "today",
                "tháng này", "thang nay", "tuần này", "tuan nay", "năm nay", "nam nay"
            ],
            [ChatIntent.SalesTrends] =
            [
                "xu hướng", "xu huong", "trend", "tăng trưởng", "tang truong", "so sánh", "so sanh",
                "tháng cao nhất", "bán chạy", "ban chay", "top", "hiệu suất", "hieu suat"
            ],
            [ChatIntent.InventoryAnalysis] =
            [
                "tồn kho", "ton kho", "nguyên liệu", "nguyen lieu", "inventory", "sắp hết", "sap het",
                "low stock", "cảnh báo", "canh bao", "nhập thêm"
            ],
            [ChatIntent.OrderManagement] =
            [
                "đơn hàng", "don hang", "order", "pending", "chờ", "cho", "xác nhận", "xac nhan",
                "hủy", "huy", "confirmed", "trạng thái", "trang thai"
            ],
            [ChatIntent.ProductRecommendation] =
            [
                "sản phẩm", "san pham", "bánh", "banh", "bán chạy", "ban chay", "bán kém", "ban kem",
                "top", "cake", "product"
            ],
            [ChatIntent.CustomerInsights] =
            [
                "khách hàng", "khach hang", "customer", "hành vi", "hanh vi", "mua nhiều"
            ]
        };

        public IReadOnlyList<ChatIntent> DetectIntents(
            AiChatMode mode,
            string userMessage,
            IReadOnlyList<ChatMessage> conversationHistory)
        {
            var combined = BuildSemanticText(userMessage, conversationHistory);
            var profiles = mode == AiChatMode.Admin ? AdminProfiles : CustomerProfiles;
            var scores = new Dictionary<ChatIntent, double>();

            foreach (var (intent, phrases) in profiles)
            {
                scores[intent] = ScoreSemanticMatch(combined, phrases);
            }

            var top = scores
                .Where(x => x.Value >= 0.35)
                .OrderByDescending(x => x.Value)
                .Take(3)
                .Select(x => x.Key)
                .ToList();

            if (top.Count == 0)
                top.Add(ChatIntent.General);

            return top;
        }

        public ChatQueryAction DetectQueryAction(
            AiChatMode mode,
            string userMessage,
            IReadOnlyList<ChatMessage> conversationHistory)
        {
            var text = BuildSemanticText(userMessage, conversationHistory);

            if (mode == AiChatMode.Admin)
                return DetectAdminAction(text);

            return DetectCustomerAction(text);
        }

        private static ChatQueryAction DetectCustomerAction(string text)
        {
            if (Score(text, ["đắt nhất", "dat nhat", "cao nhất", "cao nhat", "expensive", "giá cao nhat"]) >= 0.5)
                return ChatQueryAction.GetHighestPriceProduct;
            if (Score(text, ["rẻ nhất", "re nhat", "giá thấp", "gia thap", "cheapest"]) >= 0.5)
                return ChatQueryAction.GetLowestPriceProduct;
            if (Score(text, ["bán chạy", "ban chay", "hot", "phổ biến", "pho bien", "yêu thích", "yeu thich", "best sell"]) >= 0.5)
                return ChatQueryAction.GetTopSellingProducts;
            if (Score(text, ["giao hàng", "giao hang", "ship", "delivery", "bao lâu nhận", "bao lau nhan", "vận chuyển"]) >= 0.5)
                return ChatQueryAction.GetDeliveryInfo;
            if (Score(text, ["đặt hàng", "dat hang", "cách mua", "cach mua", "thanh toán", "cod", "giỏ hàng"]) >= 0.5)
                return ChatQueryAction.GetOrderGuide;
            if (Score(text, ["khuyến mãi", "khuyen mai", "giảm giá", "giam gia", "ưu đãi"]) >= 0.5)
                return ChatQueryAction.GetPromotionInfo;
            if (Score(text, ["liên hệ", "lien he", "hotline", "email", "gọi"]) >= 0.5)
                return ChatQueryAction.GetContactInfo;
            if (Score(text, ["dưới", "duoi", "tầm", "k ", "ngân sách", "budget"]) >= 0.4)
                return ChatQueryAction.SearchProductsByBudget;
            if (Score(text, ["giá", "gia", "price", "bao nhiêu"]) >= 0.4)
                return ChatQueryAction.GetProductCatalog;

            return ChatQueryAction.General;
        }

        private static ChatQueryAction DetectAdminAction(string text)
        {
            if (Score(text, ["bán chạy", "ban chay", "top", "best"]) >= 0.5 && Score(text, ["kém", "kem", "worst", "thấp"]) < 0.3)
                return ChatQueryAction.GetTopSellingProducts;
            if (Score(text, ["bán kém", "ban kem", "worst", "ít bán"]) >= 0.5)
                return ChatQueryAction.GetWorstSellingProducts;
            if (Score(text, ["bao nhiêu bánh", "ban duoc bao nhieu", "số bánh", "so banh", "mấy chiếc"]) >= 0.5)
                return ChatQueryAction.GetCakesSoldToday;
            if (Score(text, ["pending", "chờ xử lý", "cho xu ly", "đơn chờ"]) >= 0.5)
                return ChatQueryAction.GetPendingOrders;
            if (Score(text, ["tồn kho", "ton kho", "nguyên liệu", "sap het", "sắp hết"]) >= 0.5)
                return ChatQueryAction.GetLowStockIngredients;
            if (Score(text, ["trạng thái đơn", "trang thai don", "order status"]) >= 0.5)
                return ChatQueryAction.GetOrderStatusSummary;
            if (Score(text, ["năm nay", "nam nay", "this year"]) >= 0.5)
                return ChatQueryAction.GetRevenueYear;
            if (Score(text, ["tháng này", "thang nay", "this month"]) >= 0.5)
                return ChatQueryAction.GetRevenueMonth;
            if (Score(text, ["tuần", "tuan", "7 ngày", "week"]) >= 0.5)
                return ChatQueryAction.GetRevenueWeek;
            if (Score(text, ["hôm nay", "hom nay", "today", "doanh thu", "doanh so", "bán được", "kiếm được", "revenue"]) >= 0.4)
                return ChatQueryAction.GetRevenueToday;

            return ChatQueryAction.GetRevenueSummary;
        }

        private static double Score(string text, string[] phrases)
        {
            double max = 0;
            foreach (var p in phrases)
            {
                var n = Normalize(p);
                if (text.Contains(n, StringComparison.Ordinal)) max = Math.Max(max, 1.0);
                else
                {
                    var tokens = Tokenize(n);
                    var textTokens = Tokenize(text);
                    if (tokens.Length > 0)
                        max = Math.Max(max, (double)tokens.Count(t => textTokens.Contains(t)) / tokens.Length);
                }
            }
            return max;
        }

        private static string BuildSemanticText(string userMessage, IReadOnlyList<ChatMessage> history)
        {
            var sb = new StringBuilder();
            sb.Append(Normalize(userMessage));

            foreach (var msg in history.TakeLast(6))
                sb.Append(' ').Append(Normalize(msg.Content));

            return sb.ToString();
        }

        private static double ScoreSemanticMatch(string text, string[] phrases)
        {
            double max = 0;
            var textTokens = Tokenize(text);

            foreach (var phrase in phrases)
            {
                var normalized = Normalize(phrase);
                if (text.Contains(normalized, StringComparison.Ordinal))
                {
                    max = Math.Max(max, 1.0);
                    continue;
                }

                var phraseTokens = Tokenize(normalized);
                if (phraseTokens.Length == 0) continue;

                var overlap = phraseTokens.Count(t => textTokens.Contains(t));
                var score = (double)overlap / phraseTokens.Length;
                max = Math.Max(max, score);
            }

            return max;
        }

        private static string[] Tokenize(string text) =>
            Regex.Split(Normalize(text), @"\W+").Where(t => t.Length > 1).Distinct().ToArray();

        private static string Normalize(string input)
        {
            var lower = input.Trim().ToLowerInvariant();
            lower = lower.Replace('đ', 'd');
            return Regex.Replace(lower, @"\s+", " ");
        }
    }
}
