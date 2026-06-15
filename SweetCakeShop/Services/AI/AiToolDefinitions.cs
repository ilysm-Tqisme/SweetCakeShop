using System.Text.Json;

namespace SweetCakeShop.Services.AI
{
    /// <summary>Tool schemas for Gemini functionDeclarations and OpenAI tools.</summary>
    public static class AiToolDefinitions
    {
        public static readonly string[] CustomerFunctionNames =
        [
            "GetCheapestProduct",
            "GetHighestPriceProduct",
            "GetTopSellingProduct",
            "SearchProducts",
            "RecommendProducts",
            "GetProductList",
            "GetProductDetails",
            "GetRelatedProducts",
            "GetCategories",
            "GetDeliveryInformation",
            "GetPaymentInformation",
            "GetPromotionInformation",
            "GetCheckoutGuide",
            "GetCartSummary",
            "AddToCart",
            "GeneralConsultation"
        ];

        public static readonly string[] AdminFunctionNames =
        [
            "GetTodayRevenue",
            "GetWeeklyRevenue",
            "GetMonthlyRevenue",
            "GetYearlyRevenue",
            "GetAverageOrderValue",
            "GetPendingOrders",
            "GetInventoryAlerts",
            "GetRevenueSummary",
            "GetRevenueGrowth",
            "GetWorstSellingProduct",
            "GetCakesSoldToday",
            "GetTopCustomers",
            "GetIngredientsOverview",
            "GetTopSellingProduct",
            "GetCheapestProduct",
            "GetHighestPriceProduct",
            "SearchProducts",
            "GetProductList",
            "GeneralConsultation"
        ];

        public static object[] BuildOpenAiTools(bool isAdmin)
        {
            var names = isAdmin ? AdminFunctionNames : CustomerFunctionNames;
            return names.Select(name => new
            {
                type = "function",
                function = new
                {
                    name,
                    description = GetDescription(name),
                    parameters = GetParametersSchema(name)
                }
            }).Cast<object>().ToArray();
        }

        public static List<object> BuildGeminiTools(bool isAdmin)
        {
            var names = isAdmin ? AdminFunctionNames : CustomerFunctionNames;
            return
            [
                new
                {
                    functionDeclarations = names.Select(name => new
                    {
                        name,
                        description = GetDescription(name),
                        parameters = GetParametersSchema(name)
                    }).ToArray()
                }
            ];
        }

        private static string GetDescription(string name) => name switch
        {
            "GetCheapestProduct" => "Lowest priced cake in the shop (cheapest, least expensive, rẻ nhất, moins cher, billigsten).",
            "GetHighestPriceProduct" => "Most expensive premium cake (đắt nhất, highest price).",
            "GetTopSellingProduct" => "Best-selling cakes by confirmed order quantity.",
            "SearchProducts" => "Search catalog by keyword: chocolate/socola, fruit, birthday, etc.",
            "RecommendProducts" => "Recommend cakes for occasion, taste, or budget.",
            "GetProductList" => "List available cakes (catalog, danh sách bánh).",
            "GetProductDetails" => "Details for one cake; use productName from context when user says it/that.",
            "GetRelatedProducts" => "Similar cakes in same category as a reference product.",
            "GetCategories" => "List all cake categories on the website.",
            "GetDeliveryInformation" => "Shipping and delivery policy and timing.",
            "GetPaymentInformation" => "Payment methods: COD and Stripe online checkout.",
            "GetPromotionInformation" => "Current promotions and how to ask about discounts.",
            "GetCheckoutGuide" => "Step-by-step how to order: browse, cart, login, checkout, pay.",
            "GetCartSummary" => "Current session cart items and total.",
            "AddToCart" => "Add focused or named product to session cart with quantity.",
            "GetRevenueGrowth" => "Compare revenue periods for growth insight (admin).",
            "GetWorstSellingProduct" => "Lowest selling products (admin).",
            "GetCakesSoldToday" => "How many cakes/units sold today (quantity from confirmed orders).",
            "GetTopCustomers" => "Customers who bought the most (by revenue/order count).",
            "GetIngredientsOverview" => "Ingredient inventory levels (admin stock).",
            "GetTodayRevenue" => "Revenue today (admin only).",
            "GetWeeklyRevenue" => "Revenue last 7 days (admin only).",
            "GetMonthlyRevenue" => "Revenue this month (admin only).",
            "GetYearlyRevenue" => "Revenue this year (admin only).",
            "GetAverageOrderValue" => "Average order value (admin only).",
            "GetPendingOrders" => "Count of pending orders (admin only).",
            "GetInventoryAlerts" => "Low stock ingredients (admin only).",
            "GetRevenueSummary" => "Business overview revenue (admin only).",
            "GeneralConsultation" => "General bakery advice when no specific data function fits; still use other tools if data is needed.",
            _ => "SweetCakeShop data function."
        };

        private static object GetParametersSchema(string name)
        {
            object Props(params (string n, string t, string d, bool req)[] fields)
            {
                var props = new Dictionary<string, object>();
                var required = new List<string>();
                foreach (var (n, t, d, req) in fields)
                {
                    props[n] = new { type = t, description = d };
                    if (req) required.Add(n);
                }
                return new
                {
                    type = "object",
                    properties = props,
                    required = required.Count > 0 ? required : null
                };
            }

            return name switch
            {
                "GetTopSellingProduct" => Props(("limit", "integer", "How many products (default 5)", false)),
                "SearchProducts" => Props(("query", "string", "Search terms e.g. chocolate, socola, fruit", true), ("limit", "integer", "Max results", false)),
                "RecommendProducts" => Props(
                    ("occasion", "string", "birthday, wedding, girlfriend gift, party", false),
                    ("flavor", "string", "chocolate, fruit, less sweet, etc.", false),
                    ("maxPrice", "number", "Budget cap VND", false),
                    ("limit", "integer", "Max results", false)),
                "GetCategories" => Props(),
                "GetCheckoutGuide" => Props(),
                "GetCartSummary" => Props(),
                "GetProductList" => Props(("limit", "integer", "Max products default 12", false)),
                "GetProductDetails" => Props(("productName", "string", "Cake name; leave empty to use conversation focus", false)),
                "GetRelatedProducts" => Props(("productName", "string", "Reference cake; empty = conversation focus", false), ("limit", "integer", "Max results", false)),
                "AddToCart" => Props(("productName", "string", "Cake name; empty = last discussed product", false), ("quantity", "integer", "Quantity default 1", false)),
                "GeneralConsultation" => Props(("topic", "string", "Short topic summary", false)),
                _ => new { type = "object", properties = new { } }
            };
        }

        public static int GetIntArg(Dictionary<string, object?> args, string key, int defaultValue)
        {
            if (!args.TryGetValue(key, out var v) || v == null) return defaultValue;
            if (v is JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i)) return i;
                if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out i)) return i;
            }
            if (v is int ii) return ii;
            if (int.TryParse(v.ToString(), out var parsed)) return parsed;
            return defaultValue;
        }

        public static string GetStringArg(Dictionary<string, object?> args, string key, string defaultValue = "")
        {
            if (!args.TryGetValue(key, out var v) || v == null) return defaultValue;
            if (v is JsonElement el && el.ValueKind == JsonValueKind.String)
                return el.GetString() ?? defaultValue;
            return v.ToString()?.Trim() ?? defaultValue;
        }
    }
}
