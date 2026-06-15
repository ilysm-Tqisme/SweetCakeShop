using SweetCakeShop.Constants;
using SweetCakeShop.Helpers;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI
{
    public class AIContextBuilderService : IAIContextBuilderService
    {
        private readonly IProductAnalyticsService _products;
        private readonly IRevenueAnalyticsService _revenue;
        private readonly IInventoryService _inventory;
        private readonly IOrderAnalyticsService _orders;
        private readonly IRecommendationService _recommendations;
        private readonly IConversationMemoryService _memory;
        private readonly CartService _cart;
        private readonly IChatRoleGuardService _roleGuard;
        private readonly ICartIntentService _cartIntent;

        public AIContextBuilderService(
            IProductAnalyticsService products,
            IRevenueAnalyticsService revenue,
            IInventoryService inventory,
            IOrderAnalyticsService orders,
            IRecommendationService recommendations,
            IConversationMemoryService memory,
            CartService cart,
            IChatRoleGuardService roleGuard,
            ICartIntentService cartIntent)
        {
            _products = products;
            _revenue = revenue;
            _inventory = inventory;
            _orders = orders;
            _recommendations = recommendations;
            _memory = memory;
            _cart = cart;
            _roleGuard = roleGuard;
            _cartIntent = cartIntent;
        }

        public async Task<AiBusinessContextDto> BuildAsync(
            AiChatMode mode,
            AiIntentType intent,
            string userMessage,
            string languageCode,
            IReadOnlyList<ChatMessage> history,
            string? pageContext,
            CancellationToken cancellationToken = default)
        {
            var ctx = new AiBusinessContextDto
            {
                Intent = intent,
                UserMessage = userMessage.Trim(),
                LanguageCode = languageCode,
                PageContext = pageContext
            };

            var focus = _memory.GetSessionState(mode).Focus;

            if (mode == AiChatMode.Customer)
                intent = _roleGuard.RestrictIntentForCustomer(intent);

            switch (intent)
            {
                case AiIntentType.GetHighestPriceProduct:
                    await FillHighest(ctx, cancellationToken);
                    break;
                case AiIntentType.GetLowestPriceProduct:
                    await FillLowest(ctx, cancellationToken);
                    break;
                case AiIntentType.GetTopSellingProduct:
                    await FillTopSelling(ctx, userMessage, cancellationToken);
                    break;
                case AiIntentType.ProductTasteConsultation:
                    await FillTasteConsultation(ctx, userMessage, cancellationToken);
                    break;
                case AiIntentType.AddToCart:
                    await _cartIntent.TryAddFocusedProductAsync(userMessage, focus, ctx, cancellationToken);
                    if (!ctx.HasData) FillCart(ctx);
                    break;
                case AiIntentType.TopCustomers:
                    await FillTopCustomers(ctx, cancellationToken);
                    break;
                case AiIntentType.GetWorstSellingProduct:
                    ctx.Products = (await _products.GetWorstSellingAsync(ct: cancellationToken)).ToList();
                    ctx.HasData = ctx.Products.Count > 0;
                    break;
                case AiIntentType.GetTodayRevenue:
                    await FillRevenuePeriod(ctx, RevenueDateFilter.Today, cancellationToken);
                    break;
                case AiIntentType.GetWeeklyRevenue:
                    await FillRevenuePeriod(ctx, RevenueDateFilter.Last7Days, cancellationToken);
                    break;
                case AiIntentType.GetMonthlyRevenue:
                    await FillRevenuePeriod(ctx, RevenueDateFilter.ThisMonth, cancellationToken);
                    break;
                case AiIntentType.GetYearlyRevenue:
                    await FillRevenuePeriod(ctx, RevenueDateFilter.ThisYear, cancellationToken);
                    break;
                case AiIntentType.GetRevenueSummary:
                    ctx.Revenue = await _revenue.GetSnapshotAsync(cancellationToken);
                    ctx.Orders = await _orders.GetOrderMetricsAsync(cancellationToken);
                    ctx.Products = (await _products.GetTopSellingAsync(3, cancellationToken)).ToList();
                    ctx.HasData = true;
                    break;
                case AiIntentType.GetTotalOrders:
                    ctx.Orders = await _orders.GetOrderMetricsAsync(cancellationToken);
                    ctx.Facts.Add(new ContextFact { Key = "TotalConfirmedOrders", Value = ctx.Orders.TotalConfirmed.ToString() });
                    ctx.HasData = true;
                    break;
                case AiIntentType.GetAverageOrderValue:
                    ctx.Orders = await _orders.GetOrderMetricsAsync(cancellationToken);
                    ctx.Facts.Add(new ContextFact { Key = "AverageOrderValue", Value = $"{ctx.Orders.AverageOrderValue:N0} VND" });
                    ctx.HasData = true;
                    break;
                case AiIntentType.PendingOrders:
                    var pending = await _orders.GetPendingCountAsync(cancellationToken);
                    ctx.Orders = new OrderFactDto { Pending = pending };
                    ctx.Facts.Add(new ContextFact { Key = "PendingOrders", Value = pending.ToString() });
                    ctx.HasData = true;
                    break;
                case AiIntentType.OrderStatusSummary:
                    ctx.Orders = await _orders.GetOrderMetricsAsync(cancellationToken);
                    ctx.Orders.StatusBreakdown = await _orders.GetStatusBreakdownAsync(cancellationToken);
                    ctx.HasData = true;
                    break;
                case AiIntentType.LowInventoryProducts:
                    ctx.LowInventory = (await _inventory.GetLowStockAsync(ct: cancellationToken)).ToList();
                    ctx.HasData = ctx.LowInventory.Count > 0;
                    break;
                case AiIntentType.RecommendProduct:
                case AiIntentType.ProductConsultation:
                    ctx.Products = (await _recommendations.RecommendAsync(userMessage, cancellationToken)).ToList();
                    ctx.HasData = ctx.Products.Count > 0;
                    break;
                case AiIntentType.DeliveryQuestion:
                    FillStatic(ctx, "Delivery", "Có giao hàng nội thành", "LeadTime", "2-3 ngày làm việc sau xác nhận đơn");
                    break;
                case AiIntentType.PaymentQuestion:
                    FillStatic(ctx, "Payment", "COD hoặc Stripe Online", "Checkout", "Giỏ hàng → Thanh toán");
                    break;
                case AiIntentType.PromotionQuestion:
                    FillStatic(ctx, "Promotion", "Ưu đãi theo mùa trên website", "Contact", "1900-SWEET");
                    break;
                case AiIntentType.OrderGuide:
                    FillStatic(ctx, "OrderSteps", "Chọn sản phẩm → Giỏ → Checkout", "Payment", "COD hoặc Online");
                    break;
                case AiIntentType.ContactInfo:
                    FillStatic(ctx, "Hotline", "1900-SWEET", "Email", "support@sweetcakeshop.vn");
                    break;
                case AiIntentType.CartAssistance:
                    FillCart(ctx);
                    break;
                case AiIntentType.ProductPriceLookup:
                    await FillPriceLookup(ctx, focus, cancellationToken);
                    break;
                default:
                    if (mode == AiChatMode.Admin)
                    {
                        ctx.Revenue = await _revenue.GetSnapshotAsync(cancellationToken);
                        ctx.Orders = await _orders.GetOrderMetricsAsync(cancellationToken);
                        ctx.HasData = true;
                    }
                    else
                    {
                        ctx.Products = (await _products.GetCatalogAsync(ct: cancellationToken)).ToList();
                        ctx.HasData = ctx.Products.Count > 0;
                    }
                    break;
            }

            SetFocusFromContext(ctx);

            if (mode == AiChatMode.Customer)
                _roleGuard.SanitizeCustomerContext(ctx);

            return ctx;
        }

        private async Task FillTasteConsultation(AiBusinessContextDto ctx, string userMessage, CancellationToken ct)
        {
            var take = ChatIntentHelper.ExtractTopCount(userMessage, 5);
            var top = await _products.GetTopSellingAsync(take, ct);
            var premium = await _products.GetHighestPriceAsync(ct);
            ctx.Products = top.ToList();
            if (premium != null && ctx.Products.All(p => p.Name != premium.Name))
                ctx.Products.Insert(0, premium);
            ctx.Facts.Add(new ContextFact { Key = "ConsultationStyle", Value = "Warm bakery staff — all cakes are good; highlight top sellers from data." });
            ctx.HasData = ctx.Products.Count > 0;
        }

        private async Task FillTopCustomers(AiBusinessContextDto ctx, CancellationToken ct)
        {
            var customers = await _orders.GetTopCustomersAsync(5, ct);
            for (var i = 0; i < customers.Count; i++)
            {
                var c = customers[i];
                ctx.Facts.Add(new ContextFact
                {
                    Key = $"TopCustomer_{i + 1}",
                    Value = $"{c.CustomerName} — {c.OrderCount} đơn, {c.TotalSpent:N0} VND"
                });
            }
            ctx.HasData = customers.Count > 0;
        }

        private async Task FillHighest(AiBusinessContextDto ctx, CancellationToken ct)
        {
            var p = await _products.GetHighestPriceAsync(ct);
            if (p == null) return;
            ctx.Products = [p];
            ctx.FocusProductName = p.Name;
            ctx.FocusProductPrice = p.Price;
            ctx.HasData = true;
        }

        private async Task FillLowest(AiBusinessContextDto ctx, CancellationToken ct)
        {
            var p = await _products.GetLowestPriceAsync(ct);
            if (p == null) return;
            ctx.Products = [p];
            ctx.FocusProductName = p.Name;
            ctx.FocusProductPrice = p.Price;
            ctx.HasData = true;
        }

        private async Task FillTopSelling(AiBusinessContextDto ctx, string userMessage, CancellationToken ct)
        {
            var take = ChatIntentHelper.ExtractTopCount(userMessage, 5);
            ctx.Products = (await _products.GetTopSellingAsync(take, ct)).ToList();
            if (ctx.Products.Count > 0)
            {
                ctx.FocusProductName = ctx.Products[0].Name;
                ctx.FocusProductPrice = ctx.Products[0].Price;
            }
            ctx.HasData = ctx.Products.Count > 0;
        }

        private async Task FillRevenuePeriod(AiBusinessContextDto ctx, RevenueDateFilter filter, CancellationToken ct)
        {
            var (amount, orders, label) = await _revenue.GetForPeriodAsync(filter, ct);
            ctx.Revenue = new RevenueFactDto { Filtered = amount, FilterLabel = label };
            ctx.Orders = new OrderFactDto { TotalConfirmed = orders };
            ctx.Facts.Add(new ContextFact { Key = "PeriodRevenue", Value = $"{amount:N0} VND" });
            ctx.Facts.Add(new ContextFact { Key = "PeriodOrders", Value = orders.ToString() });
            ctx.HasData = true;
        }

        private async Task FillPriceLookup(AiBusinessContextDto ctx, ConversationFocus? focus, CancellationToken ct)
        {
            ProductFactDto? p = null;
            if (!string.IsNullOrWhiteSpace(focus?.ProductName))
                p = await _products.FindByNameAsync(focus.ProductName, ct);

            if (p != null)
            {
                ctx.Products = [p];
                ctx.FocusProductName = p.Name;
                ctx.FocusProductPrice = p.Price;
                ctx.HasData = true;
            }
        }

        private void FillCart(AiBusinessContextDto ctx)
        {
            var cart = _cart.GetCart();
            ctx.Cart = new CartFactDto
            {
                ItemCount = cart.Items.Sum(i => i.Quantity),
                Total = cart.TotalAmount
            };
            ctx.HasData = true;
        }

        private static void FillStatic(AiBusinessContextDto ctx, string k1, string v1, string k2, string v2)
        {
            ctx.Facts.Add(new ContextFact { Key = k1, Value = v1 });
            ctx.Facts.Add(new ContextFact { Key = k2, Value = v2 });
            ctx.HasData = true;
        }

        private static void SetFocusFromContext(AiBusinessContextDto ctx)
        {
            if (!string.IsNullOrWhiteSpace(ctx.FocusProductName)) return;
            if (ctx.Products.Count > 0)
            {
                ctx.FocusProductName = ctx.Products[0].Name;
                ctx.FocusProductPrice = ctx.Products[0].Price;
            }
        }
    }
}
