using SweetCakeShop.Constants;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    /// <summary>Fallback answers phong cách nhân viên — dùng khi LLM không phản hồi.</summary>
    public class StructuredAiResponseService : IStructuredAiResponseService
    {
        public string Compose(AiChatMode mode, AiBusinessContextDto ctx)
        {
            if (ctx.Facts.Any(f => f.Key == "AccessDenied"))
                return ctx.Facts.First(f => f.Key == "AccessDenied").Value;

            if (!ctx.HasData)
                return NoData(mode, ctx.LanguageCode);

            var fn = string.IsNullOrWhiteSpace(ctx.PrimaryExecutedFunction)
                ? ctx.Facts.FirstOrDefault(f => f.Key == "ExecutedFunction")?.Value ?? ""
                : ctx.PrimaryExecutedFunction;

            return fn switch
            {
                "GetCheapestProduct" => FormatSingleProduct(ctx, cheapest: true),
                "GetHighestPriceProduct" => FormatSingleProduct(ctx, cheapest: false),
                "GetTopSellingProduct" => FormatTopSellingConsultant(ctx),
                "SearchProducts" or "RecommendProducts" or "GetProductList" => FormatRecommend(ctx),
                "GetProductDetails" => FormatPriceLookup(ctx),
                "GetRelatedProducts" => FormatRecommend(ctx, "Bánh cùng dòng tương tự:"),
                "GetCakesSoldToday" => FormatCakesSoldToday(ctx),
                "GetTopCustomers" => FormatTopCustomers(ctx),
                "GetIngredientsOverview" => FormatAllIngredients(ctx),
                "GetInventoryAlerts" => FormatInventory(ctx),
                "GetTodayRevenue" or "GetWeeklyRevenue" or "GetMonthlyRevenue" or "GetYearlyRevenue" => FormatPeriodRevenue(ctx),
                "GetRevenueSummary" => FormatRevenueSummary(ctx),
                "GetRevenueGrowth" => FormatGrowth(ctx),
                "GetAverageOrderValue" => $"Dạ, giá trị trung bình mỗi đơn: **{ctx.Orders?.AverageOrderValue:N0} VND** ạ.",
                "GetPendingOrders" => $"Dạ, hiện có **{ctx.Orders?.Pending ?? 0}** đơn đang chờ xử lý ạ.",
                "GetCartSummary" => FormatCart(ctx),
                "AddToCart" => FormatAddToCart(ctx),
                "GetDeliveryInformation" or "GetDeliveryInfo" => $"Dạ, {Fact(ctx, "Delivery")} ạ.",
                "GetPaymentInformation" or "GetPaymentInfo" => $"Dạ, thanh toán: {Fact(ctx, "Payment")} ạ.",
                "GetPromotionInformation" or "GetPromotionInfo" => $"Dạ, {Fact(ctx, "Promotion")} ạ.",
                "GetCheckoutGuide" => $"Dạ, đặt hàng: {Fact(ctx, "CheckoutSteps")} ạ.",
                _ => FormatGeneric(mode, ctx)
            };
        }

        private static string FormatTopSellingConsultant(AiBusinessContextDto ctx)
        {
            if (ctx.Products.Count == 0) return NoData(AiChatMode.Customer, ctx.LanguageCode);
            var best = ctx.Products[0];
            return $"Dạ, **{best.Name}** 🎂 đang bán chạy nhất — **{best.Price:N0} VND**, đã bán **{best.SoldQuantity}** phần ạ.";
        }

        private static string FormatSingleProduct(AiBusinessContextDto ctx, bool cheapest)
        {
            var p = ctx.Products.FirstOrDefault();
            if (p == null) return NoData(AiChatMode.Customer, ctx.LanguageCode);
            var cat = string.IsNullOrWhiteSpace(p.Category) ? "" : $" ({p.Category})";
            return cheapest
                ? $"Dạ, bánh rẻ nhất hiện tại là **{p.Name}** 🍰 — **{p.Price:N0} VND**{cat} ạ."
                : $"Dạ, bánh đắt nhất là **{p.Name}** 🎂 — **{p.Price:N0} VND**{cat} ạ.";
        }

        private static string FormatCakesSoldToday(AiBusinessContextDto ctx)
        {
            var qty = Fact(ctx, "CakesSoldToday");
            var rev = Fact(ctx, "RevenueToday");
            var ord = Fact(ctx, "OrdersToday");
            return $"Dạ, hôm nay shop đã bán **{qty}** phần bánh (từ đơn Confirmed/Completed). Doanh thu **{rev}**, **{ord}** đơn ạ.";
        }

        private static string FormatRecommend(AiBusinessContextDto ctx, string? title = null)
        {
            var head = title ?? "Em gợi ý các mẫu phù hợp:";
            var lines = ctx.Products.Select(p =>
            {
                var cat = string.IsNullOrWhiteSpace(p.Category) ? "" : $" ({p.Category})";
                return $"• **{p.Name}** 🍰 — {p.Price:N0} VND{cat}";
            });
            return $"Dạ, {head}\n{string.Join("\n", lines)}";
        }

        private static string FormatPeriodRevenue(AiBusinessContextDto ctx)
        {
            var label = ctx.Revenue?.FilterLabel ?? "Kỳ";
            var amt = ctx.Revenue?.Filtered ?? 0;
            var orders = ctx.Orders?.TotalConfirmed ?? 0;
            var cakes = Fact(ctx, "CakesSoldToday");
            var extra = !string.IsNullOrEmpty(cakes) ? $" Số bánh bán: **{cakes}** phần." : "";
            return $"Dạ, **{label}**: doanh thu **{amt:N0} VND**, **{orders}** đơn xác nhận/hoàn tất.{extra}";
        }

        private static string FormatRevenueSummary(AiBusinessContextDto ctx)
        {
            var r = ctx.Revenue!;
            return $"""
                Dạ, tổng quan kinh doanh SweetCakeShop:
                • Hôm nay: {r.Today:N0} VND
                • Tuần: {r.Week:N0} VND
                • Tháng: {r.Month:N0} VND
                • Năm: {r.Year:N0} VND
                • Đơn xác nhận: {ctx.Orders?.TotalConfirmed ?? 0} | TB/đơn: {ctx.Orders?.AverageOrderValue:N0} VND
                """;
        }

        private static string FormatGrowth(AiBusinessContextDto ctx) =>
            $"Dạ, tháng này **{Fact(ctx, "RevenueThisMonth")}**, tháng trước **{Fact(ctx, "RevenueLastMonth")}**, tăng trưởng **{Fact(ctx, "GrowthPercent")}** ạ.";

        private static string FormatInventory(AiBusinessContextDto ctx)
        {
            if (ctx.LowInventory.Count == 0) return "Dạ, không có nguyên liệu cảnh báo sắp hết ạ.";
            var lines = ctx.LowInventory.Where(i => i.Quantity <= 5)
                .Select(i => $"• {i.Name}: {i.Quantity} {i.Unit}");
            return "Dạ, nguyên liệu cần chú ý (tồn thấp):\n" + string.Join("\n", lines);
        }

        private static string FormatAllIngredients(AiBusinessContextDto ctx)
        {
            var lines = ctx.LowInventory.Select(i => $"• {i.Name}: {i.Quantity} {i.Unit}");
            return "Dạ, tồn kho nguyên liệu:\n" + string.Join("\n", lines);
        }

        private static string FormatTopCustomers(AiBusinessContextDto ctx)
        {
            var lines = ctx.Facts.Where(f => f.Key.StartsWith("TopCustomer_")).Select(f => $"• {f.Value}");
            return lines.Any()
                ? "Dạ, khách mua nhiều nhất:\n" + string.Join("\n", lines)
                : "Dạ, chưa có dữ liệu khách hàng cho kỳ này ạ.";
        }

        private static string FormatCart(AiBusinessContextDto ctx)
        {
            if (ctx.Cart == null || ctx.Cart.ItemCount == 0)
                return "Dạ, giỏ hàng đang trống. Anh/chị vào mục Sản phẩm chọn bánh rồi thêm vào giỏ nhé ạ.";
            return $"Dạ, giỏ có **{ctx.Cart.ItemCount}** món, tổng **{ctx.Cart.Total:N0} VND**. Vào /Cart để thanh toán ạ.";
        }

        private static string FormatPriceLookup(AiBusinessContextDto ctx)
        {
            var p = ctx.Products.FirstOrDefault();
            if (p == null) return "Dạ, anh/chị muốn hỏi giá bánh nào ạ?";
            return $"Dạ, **{p.Name}** có giá **{p.Price:N0} VND** ạ.";
        }

        private static string FormatAddToCart(AiBusinessContextDto ctx)
        {
            var action = Fact(ctx, "CartAction");
            return !string.IsNullOrEmpty(action)
                ? $"Dạ, em đã thêm vào giỏ giúp anh/chị rồi ạ! {action}. Vào Giỏ hàng để thanh toán nhé."
                : "Dạ, anh/chị cho em biết tên bánh (hoặc hỏi giá món trước) để em thêm giúp ạ.";
        }

        private static string FormatGeneric(AiChatMode mode, AiBusinessContextDto ctx)
        {
            if (mode == AiChatMode.Admin && ctx.Revenue != null) return FormatRevenueSummary(ctx);
            if (ctx.Products.Count > 0) return FormatRecommend(ctx);
            return NoData(mode, ctx.LanguageCode);
        }

        private static string Fact(AiBusinessContextDto ctx, string key) =>
            ctx.Facts.FirstOrDefault(f => f.Key == key)?.Value ?? "";

        private static string NoData(AiChatMode mode, string lang) =>
            lang == "en"
                ? "I don't have exact data for that right now — try the Products page or Dashboard."
                : mode == AiChatMode.Admin
                    ? "Dạ, chưa có dữ liệu chính xác cho câu hỏi này — anh/chị thử lọc trên Dashboard nhé."
                    : "Dạ, em chưa tra được số liệu chính xác. Anh/chị xem Sản phẩm trên web hoặc gọi 1900-SWEET ạ.";
    }
}
