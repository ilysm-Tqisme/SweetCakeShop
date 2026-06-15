using SweetCakeShop.Constants;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public class AiResponseComposer : IAiResponseComposer
    {
        public string Compose(AiChatMode mode, ChatQueryResult queryResult, string languageCode)
        {
            if (!queryResult.HasData)
                return GetNoDataMessage(mode, languageCode);

            var f = queryResult.Facts;
            return queryResult.Action switch
            {
                ChatQueryAction.GetHighestPriceProduct => ComposeHighest(f, languageCode),
                ChatQueryAction.GetLowestPriceProduct => ComposeLowest(f),
                ChatQueryAction.GetTopSellingProducts => ComposeTopSelling(mode, f, languageCode),
                ChatQueryAction.GetDeliveryInfo => ComposeDelivery(f),
                ChatQueryAction.GetOrderGuide => ComposeOrderGuide(f),
                ChatQueryAction.GetPromotionInfo => ComposePromotion(f),
                ChatQueryAction.GetContactInfo => ComposeContact(f),
                ChatQueryAction.SearchProductsByBudget => ComposeBudgetList(f),
                ChatQueryAction.GetRevenueToday or ChatQueryAction.GetRevenueWeek
                    or ChatQueryAction.GetRevenueMonth or ChatQueryAction.GetRevenueYear => ComposeRevenuePeriod(f),
                ChatQueryAction.GetRevenueSummary => ComposeRevenueSummary(f),
                ChatQueryAction.GetPendingOrders => $"Hiện có **{f.GetValueOrDefault("don_cho")}** đơn đang chờ xử lý (Pending).",
                ChatQueryAction.GetCakesSoldToday => ComposeCakesSold(f),
                ChatQueryAction.GetLowStockIngredients => ComposeLowStock(f),
                _ => ComposeGeneric(mode, f)
            };
        }

        private static string ComposeHighest(Dictionary<string, object?> f, string lang)
        {
            var name = f.GetValueOrDefault("ten_banh")?.ToString() ?? "N/A";
            var price = f.GetValueOrDefault("gia")?.ToString() ?? "";
            var cat = f.GetValueOrDefault("danh_muc")?.ToString();
            if (lang == "en")
                return $"Our highest-priced cake is **{name}** at **{price}**" + (string.IsNullOrWhiteSpace(cat) ? "." : $" ({cat}).") + " Ideal for premium celebrations.";
            return $"Hiện tại mẫu bánh cao cấp nhất là **{name}** — **{price}**" + (string.IsNullOrWhiteSpace(cat) ? "." : $" ({cat}).") + " Rất phù hợp tiệc sang trọng hoặc quà tặng.";
        }

        private static string GetNoDataMessage(AiChatMode mode, string lang)
        {
            if (lang == "en")
                return mode == AiChatMode.Admin
                    ? "No accurate data is available for this query or period. Try another filter on the Dashboard."
                    : "I couldn't retrieve exact details right now. Please browse our Products page or call 1900-SWEET.";
            return mode == AiChatMode.Admin
                ? "Không có dữ liệu chính xác cho truy vấn hoặc khoảng thời gian này."
                : "Hiện chưa có dữ liệu chính xác. Anh/chị xem mục Sản phẩm hoặc gọi 1900-SWEET nhé.";
        }

        private static string ComposeLowest(Dictionary<string, object?> f) =>
            $"Dạ, mẫu bánh có giá mềm nhất hiện tại là **{f.GetValueOrDefault("ten_banh")}** — **{f.GetValueOrDefault("gia")}** ạ.";

        private static string ComposeTopSelling(AiChatMode mode, Dictionary<string, object?> f, string lang)
        {
            var best = f.GetValueOrDefault("ban_chay_nhat")?.ToString();
            var lines = f.Where(kv => kv.Key.StartsWith("top_")).OrderBy(kv => kv.Key)
                .Select(kv => $"• {kv.Value}").ToList();
            if (lang == "en")
                return $"Best seller: **{best}**.\n" + string.Join("\n", lines);
            return $"Bán chạy nhất: **{best}**.\n" + string.Join("\n", lines);
        }

        private static string ComposeDelivery(Dictionary<string, object?> f) =>
            $"Dạ, {f.GetValueOrDefault("giao_hang")}. Thời gian giao khoảng **{f.GetValueOrDefault("thoi_gian")}**. {f.GetValueOrDefault("ho_tro")} ạ.";

        private static string ComposeOrderGuide(Dictionary<string, object?> f) =>
            $"Dạ, anh/chị có thể đặt theo các bước: **{f.GetValueOrDefault("cach_dat")}**. Hỗ trợ **{f.GetValueOrDefault("thanh_toan")}** ạ.";

        private static string ComposePromotion(Dictionary<string, object?> f) =>
            $"Dạ, {f.GetValueOrDefault("khuyen_mai")}. {f.GetValueOrDefault("lien_he")} ạ.";

        private static string ComposeContact(Dictionary<string, object?> f) =>
            $"Dạ, liên hệ SweetCakeShop: Hotline **{f.GetValueOrDefault("hotline")}**, email **{f.GetValueOrDefault("email")}** ạ.";

        private static string ComposeBudgetList(Dictionary<string, object?> f)
        {
            var items = f.Where(kv => kv.Key.StartsWith("goi_y_")).OrderBy(kv => kv.Key).Select(kv => $"• {kv.Value}").ToList();
            return items.Count > 0
                ? "Dạ, em gợi ý một vài mẫu phù hợp ngân sách:\n" + string.Join("\n", items)
                : "Dạ, anh/chị xem thêm danh sách Sản phẩm trên website nhé.";
        }

        private static string ComposeRevenuePeriod(Dictionary<string, object?> f) =>
            $"**{f.GetValueOrDefault("bo_loc")}**: doanh thu **{f.GetValueOrDefault("doanh_thu_loc")}**, **{f.GetValueOrDefault("so_don")}** đơn đã xác nhận/hoàn tất.";

        private static string ComposeRevenueSummary(Dictionary<string, object?> f) =>
            $"""
             Tổng quan doanh thu SweetCakeShop:
             • Hôm nay: {f.GetValueOrDefault("doanh_thu_hom_nay")}
             • Tuần: {f.GetValueOrDefault("doanh_thu_tuan")}
             • Tháng: {f.GetValueOrDefault("doanh_thu_thang")}
             • Năm: {f.GetValueOrDefault("doanh_thu_nam")}
             • Đơn xác nhận: {f.GetValueOrDefault("don_xac_nhan")} | TB/đơn: {f.GetValueOrDefault("gia_tri_tb_don")}
             """;

        private static string ComposeCakesSold(Dictionary<string, object?> f) =>
            $"Hôm nay đã bán **{f.GetValueOrDefault("so_banh_ban_hom_nay")}** bánh (số lượng sản phẩm), doanh thu **{f.GetValueOrDefault("doanh_thu_hom_nay")}**.";

        private static string ComposeLowStock(Dictionary<string, object?> f)
        {
            var items = f.Where(kv => kv.Key.StartsWith("ton_kho_")).Select(kv => $"• {kv.Value}").ToList();
            return "Cảnh báo tồn kho thấp:\n" + string.Join("\n", items);
        }

        private static string ComposeGeneric(AiChatMode mode, Dictionary<string, object?> f)
        {
            var lines = f.Select(kv => $"• {kv.Key}: {kv.Value}").Take(8);
            return mode == AiChatMode.Admin
                ? "Dữ liệu hệ thống:\n" + string.Join("\n", lines)
                : "Dạ, thông tin từ cửa hàng:\n" + string.Join("\n", lines);
        }
    }
}
