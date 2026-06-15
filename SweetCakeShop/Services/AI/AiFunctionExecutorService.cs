using System.Text.Json;
using SweetCakeShop.Constants;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI
{
    public class AiFunctionExecutorService : IAiFunctionExecutorService
    {
        private readonly IProductAnalyticsService _products;
        private readonly IRevenueAnalyticsService _revenue;
        private readonly IInventoryService _inventory;
        private readonly IOrderAnalyticsService _orders;
        private readonly IRecommendationService _recommendations;
        private readonly IChatRoleGuardService _roleGuard;
        private readonly ICartIntentService _cartIntent;
        private readonly CartService _cart;

        private static readonly HashSet<string> AdminOnlyFunctions =
        [
            "GetTodayRevenue", "GetWeeklyRevenue", "GetMonthlyRevenue", "GetYearlyRevenue",
            "GetAverageOrderValue", "GetPendingOrders", "GetInventoryAlerts", "GetRevenueSummary",
            "GetRevenueGrowth", "GetWorstSellingProduct", "GetCakesSoldToday",
            "GetTopCustomers", "GetIngredientsOverview"
        ];

        public AiFunctionExecutorService(
            IProductAnalyticsService products,
            IRevenueAnalyticsService revenue,
            IInventoryService inventory,
            IOrderAnalyticsService orders,
            IRecommendationService recommendations,
            IChatRoleGuardService roleGuard,
            ICartIntentService cartIntent,
            CartService cart)
        {
            _products = products;
            _revenue = revenue;
            _inventory = inventory;
            _orders = orders;
            _recommendations = recommendations;
            _roleGuard = roleGuard;
            _cartIntent = cartIntent;
            _cart = cart;
        }

        public async Task<AiBusinessContextDto> ExecuteAsync(
            AiChatMode mode,
            AiFunctionPlan plan,
            string userMessage,
            ConversationSessionState sessionState,
            string languageCode,
            CancellationToken cancellationToken = default)
        {
            var ctx = new AiBusinessContextDto
            {
                UserMessage = userMessage.Trim(),
                LanguageCode = languageCode,
                Intent = AiIntentType.General
            };

            var call = plan.Calls.FirstOrDefault()
                       ?? new AiFunctionCall { Name = "GetProductList", Arguments = new() };

            ctx.PrimaryExecutedFunction = call.Name;

            if (mode == AiChatMode.Customer && AdminOnlyFunctions.Contains(call.Name))
            {
                ctx.Facts.Add(new ContextFact { Key = "AccessDenied", Value = _roleGuard.GetAccessDeniedMessage(languageCode) });
                ctx.HasData = true;
            }
            else
            {
                await ExecuteOneAsync(mode, call, ctx, sessionState, userMessage, cancellationToken);
            }

            ApplyFocus(ctx, sessionState);

            if (mode == AiChatMode.Customer)
                _roleGuard.SanitizeCustomerContext(ctx);

            ctx.HasData = ctx.Products.Count > 0 || ctx.Revenue != null || ctx.Orders != null
                || ctx.LowInventory.Count > 0 || ctx.Facts.Count > 0 || ctx.Cart != null;

            return ctx;
        }

        private async Task ExecuteOneAsync(
            AiChatMode mode,
            AiFunctionCall call,
            AiBusinessContextDto ctx,
            ConversationSessionState session,
            string userMessage,
            CancellationToken ct)
        {
            switch (call.Name)
            {
                case "GetCheapestProduct":
                    await SetSingleProduct(ctx, await _products.GetLowestPriceAsync(ct));
                    break;
                case "GetHighestPriceProduct":
                    await SetSingleProduct(ctx, await _products.GetHighestPriceAsync(ct));
                    break;
                case "GetTopSellingProduct":
                    var limit = AiToolDefinitions.GetIntArg(call.Arguments, "limit", 5);
                    ctx.Products = (await _products.GetTopSellingAsync(limit, ct)).ToList();
                    ctx.HasData = ctx.Products.Count > 0;
                    break;
                case "SearchProducts":
                    var query = AiToolDefinitions.GetStringArg(call.Arguments, "query");
                    if (string.IsNullOrWhiteSpace(query)) query = userMessage;
                    var sLimit = AiToolDefinitions.GetIntArg(call.Arguments, "limit", 8);
                    ctx.Products = (await _products.SearchProductsAsync(query, sLimit, ct)).ToList();
                    ctx.HasData = ctx.Products.Count > 0;
                    break;
                case "RecommendProducts":
                    var occasion = AiToolDefinitions.GetStringArg(call.Arguments, "occasion");
                    var flavor = AiToolDefinitions.GetStringArg(call.Arguments, "flavor");
                    if (string.IsNullOrWhiteSpace(occasion)) occasion = session.Preferences.Occasion;
                    if (string.IsNullOrWhiteSpace(flavor)) flavor = session.Preferences.FlavorOrStyle;
                    decimal? budget = ParseDecimalArg(call.Arguments, "maxPrice") ?? session.Preferences.BudgetMax;
                    var rLim = AiToolDefinitions.GetIntArg(call.Arguments, "limit", 8);
                    if (!string.IsNullOrWhiteSpace(occasion)) session.Preferences.Occasion = occasion;
                    if (!string.IsNullOrWhiteSpace(flavor)) session.Preferences.FlavorOrStyle = flavor;
                    ctx.Products = (await _recommendations.RecommendWithPreferencesAsync(
                        occasion, flavor, budget, rLim, ct)).ToList();
                    ctx.HasData = ctx.Products.Count > 0;
                    break;
                case "GetProductList":
                    var pLimit = AiToolDefinitions.GetIntArg(call.Arguments, "limit", 12);
                    ctx.Products = (await _products.GetCatalogAsync(pLimit, ct)).ToList();
                    ctx.HasData = ctx.Products.Count > 0;
                    break;
                case "GetProductDetails":
                    var detailName = ResolveProductName(call, session);
                    var detail = await _products.GetProductDetailsAsync(detailName, ct);
                    await SetSingleProduct(ctx, detail);
                    break;
                case "GetRelatedProducts":
                    var relName = ResolveProductName(call, session);
                    var rLimit = AiToolDefinitions.GetIntArg(call.Arguments, "limit", 6);
                    ctx.Products = (await _products.GetRelatedProductsAsync(relName, rLimit, ct)).ToList();
                    ctx.HasData = ctx.Products.Count > 0;
                    break;
                case "GetCategories":
                    var cats = await _products.GetCategoriesAsync(ct);
                    for (var i = 0; i < cats.Count; i++)
                        ctx.Facts.Add(new ContextFact { Key = $"Category_{i + 1}", Value = cats[i] });
                    ctx.HasData = cats.Count > 0;
                    break;
                case "GetDeliveryInformation":
                case "GetDeliveryInfo":
                    ctx.Facts.Add(new ContextFact { Key = "Delivery", Value = "Có giao hàng nội thành — 2-3 ngày làm việc sau xác nhận đơn." });
                    ctx.HasData = true;
                    break;
                case "GetPaymentInformation":
                case "GetPaymentInfo":
                    ctx.Facts.Add(new ContextFact { Key = "Payment", Value = "COD hoặc Stripe Online — Giỏ hàng → Thanh toán." });
                    ctx.HasData = true;
                    break;
                case "GetPromotionInformation":
                case "GetPromotionInfo":
                    ctx.Facts.Add(new ContextFact { Key = "Promotion", Value = "Ưu đãi theo mùa trên website — hotline 1900-SWEET." });
                    ctx.HasData = true;
                    break;
                case "GetCheckoutGuide":
                    ctx.Facts.Add(new ContextFact { Key = "CheckoutSteps", Value = "Chọn bánh → Thêm giỏ (/Cart) → Đăng nhập → Checkout → Thanh toán COD hoặc Online." });
                    ctx.HasData = true;
                    break;
                case "GetCartSummary":
                    var cartVm = _cart.GetCart();
                    ctx.Cart = new CartFactDto
                    {
                        ItemCount = cartVm.Items.Sum(i => i.Quantity),
                        Total = cartVm.TotalAmount
                    };
                    foreach (var item in cartVm.Items)
                        ctx.Facts.Add(new ContextFact { Key = "CartItem", Value = $"{item.ProductName} x{item.Quantity}" });
                    ctx.HasData = cartVm.Items.Count > 0;
                    break;
                case "GetWorstSellingProduct":
                    ctx.Products = (await _products.GetWorstSellingAsync(5, ct)).ToList();
                    ctx.HasData = ctx.Products.Count > 0;
                    break;
                case "GetRevenueGrowth":
                    var growth = await _orders.GetRevenueGrowthAsync(ct);
                    ctx.Facts.Add(new ContextFact { Key = "RevenueThisMonth", Value = $"{growth.ThisMonth:N0} VND" });
                    ctx.Facts.Add(new ContextFact { Key = "RevenueLastMonth", Value = $"{growth.LastMonth:N0} VND" });
                    ctx.Facts.Add(new ContextFact { Key = "GrowthPercent", Value = $"{growth.GrowthPercent:N1}%" });
                    ctx.HasData = true;
                    break;
                case "AddToCart":
                    await _cartIntent.TryAddFocusedProductAsync(userMessage, session.Focus, ctx, ct);
                    break;
                case "GetCakesSoldToday":
                    var cakesToday = await _orders.GetCakesSoldTodayAsync(ct);
                    var revToday = await _revenue.GetForPeriodAsync(RevenueDateFilter.Today, ct);
                    ctx.Facts.Add(new ContextFact { Key = "CakesSoldToday", Value = cakesToday.ToString() });
                    ctx.Facts.Add(new ContextFact { Key = "RevenueToday", Value = $"{revToday.Amount:N0} VND" });
                    ctx.Facts.Add(new ContextFact { Key = "OrdersToday", Value = revToday.Orders.ToString() });
                    ctx.HasData = true;
                    break;
                case "GetTopCustomers":
                    var topCust = await _orders.GetTopCustomersAsync(5, ct);
                    for (var i = 0; i < topCust.Count; i++)
                    {
                        var c = topCust[i];
                        ctx.Facts.Add(new ContextFact
                        {
                            Key = $"TopCustomer_{i + 1}",
                            Value = $"{c.CustomerName} — {c.OrderCount} đơn, {c.TotalSpent:N0} VND"
                        });
                    }
                    ctx.HasData = topCust.Count > 0;
                    break;
                case "GetIngredientsOverview":
                    ctx.LowInventory = (await _inventory.GetAllIngredientsAsync(15, ct)).ToList();
                    ctx.HasData = ctx.LowInventory.Count > 0;
                    break;
                case "GetTodayRevenue":
                    await FillRevenue(ctx, RevenueDateFilter.Today, ct);
                    var qtyToday = await _orders.GetCakesSoldTodayAsync(ct);
                    ctx.Facts.Add(new ContextFact { Key = "CakesSoldToday", Value = qtyToday.ToString() });
                    break;
                case "GetWeeklyRevenue":
                    await FillRevenue(ctx, RevenueDateFilter.Last7Days, ct);
                    break;
                case "GetMonthlyRevenue":
                    await FillRevenue(ctx, RevenueDateFilter.ThisMonth, ct);
                    break;
                case "GetYearlyRevenue":
                    await FillRevenue(ctx, RevenueDateFilter.ThisYear, ct);
                    break;
                case "GetAverageOrderValue":
                    ctx.Orders = await _orders.GetOrderMetricsAsync(ct);
                    ctx.Facts.Add(new ContextFact { Key = "AverageOrderValue", Value = $"{ctx.Orders.AverageOrderValue:N0} VND" });
                    ctx.HasData = true;
                    break;
                case "GetPendingOrders":
                    var pending = await _orders.GetPendingCountAsync(ct);
                    ctx.Orders = new OrderFactDto { Pending = pending };
                    ctx.Facts.Add(new ContextFact { Key = "PendingOrders", Value = pending.ToString() });
                    ctx.HasData = true;
                    break;
                case "GetInventoryAlerts":
                    ctx.LowInventory = (await _inventory.GetLowStockAsync(ct: ct)).ToList();
                    ctx.HasData = ctx.LowInventory.Count > 0;
                    break;
                case "GetRevenueSummary":
                    ctx.Revenue = await _revenue.GetSnapshotAsync(ct);
                    ctx.Orders = await _orders.GetOrderMetricsAsync(ct);
                    ctx.Products = (await _products.GetTopSellingAsync(3, ct)).ToList();
                    ctx.HasData = true;
                    break;
                case "GeneralConsultation":
                    ctx.Facts.Add(new ContextFact { Key = "Note", Value = "Chưa xác định truy vấn cụ thể — hãy hỏi rõ hơn." });
                    ctx.HasData = false;
                    break;
            }
        }

        private static string ResolveProductName(AiFunctionCall call, ConversationSessionState session)
        {
            var fromArgs = AiToolDefinitions.GetStringArg(call.Arguments, "productName");
            if (!string.IsNullOrWhiteSpace(fromArgs)) return fromArgs;
            return session.Focus?.ProductName ?? "";
        }

        private static async Task SetSingleProduct(AiBusinessContextDto ctx, ProductFactDto? p)
        {
            if (p == null) return;
            ctx.Products = [p];
            ctx.FocusProductName = p.Name;
            ctx.FocusProductPrice = p.Price;
            ctx.HasData = true;
            await Task.CompletedTask;
        }

        private async Task FillRevenue(AiBusinessContextDto ctx, RevenueDateFilter filter, CancellationToken ct)
        {
            var (amount, orders, label) = await _revenue.GetForPeriodAsync(filter, ct);
            ctx.Revenue = new RevenueFactDto { Filtered = amount, FilterLabel = label };
            ctx.Orders = new OrderFactDto { TotalConfirmed = orders };
            ctx.HasData = true;
        }

        private static decimal? ParseDecimalArg(Dictionary<string, object?> args, string key)
        {
            if (!args.TryGetValue(key, out var v) || v == null) return null;
            if (v is JsonElement el && el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var d))
                return d;
            if (decimal.TryParse(v.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static void ApplyFocus(AiBusinessContextDto ctx, ConversationSessionState session)
        {
            if (ctx.Products.Count > 0)
            {
                var p = ctx.Products[0];
                session.Focus = new ConversationFocus
                {
                    ProductName = p.Name,
                    ProductPrice = p.Price,
                    Category = p.Category
                };
                if (!session.RecentProductNames.Contains(p.Name))
                    session.RecentProductNames.Add(p.Name);
            }

            if (!string.IsNullOrWhiteSpace(ctx.FocusProductName))
            {
                session.Focus ??= new ConversationFocus();
                session.Focus.ProductName = ctx.FocusProductName;
                session.Focus.ProductPrice = ctx.FocusProductPrice;
            }
        }
    }
}
