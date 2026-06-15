namespace SweetCakeShop.Constants
{
    /// <summary>Actionable intents mapped to safe EF queries (function-calling pattern).</summary>
    public enum ChatQueryAction
    {
        General,
        GetHighestPriceProduct,
        GetLowestPriceProduct,
        GetTopSellingProducts,
        SearchProductsByBudget,
        GetProductCatalog,
        GetDeliveryInfo,
        GetOrderGuide,
        GetPromotionInfo,
        GetContactInfo,
        GetRevenueToday,
        GetRevenueWeek,
        GetRevenueMonth,
        GetRevenueYear,
        GetRevenueSummary,
        GetPendingOrders,
        GetOrderStatusSummary,
        GetLowStockIngredients,
        GetWorstSellingProducts,
        GetCakesSoldToday
    }
}
