using SweetCakeShop.Constants;

namespace SweetCakeShop.Models.AI
{
    public class AiBusinessContextDto
    {
        public AiIntentType Intent { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = "vi";
        public bool HasData { get; set; }
        public string? PageContext { get; set; }
        public string? FocusProductName { get; set; }
        public decimal? FocusProductPrice { get; set; }
        public List<ContextFact> Facts { get; set; } = [];
        public List<ProductFactDto> Products { get; set; } = [];
        public RevenueFactDto? Revenue { get; set; }
        public OrderFactDto? Orders { get; set; }
        public List<InventoryFactDto> LowInventory { get; set; } = [];
        public CartFactDto? Cart { get; set; }
        public string PrimaryExecutedFunction { get; set; } = string.Empty;

        public string ToSystemContextText()
        {
            if (!HasData && Facts.Count == 0 && Products.Count == 0 && Revenue == null)
                return "Status: No matching business data was found for this query.";

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(PrimaryExecutedFunction))
                lines.Add($"QueryType: {PrimaryExecutedFunction}");

            if (!string.IsNullOrWhiteSpace(FocusProductName))
                lines.Add($"ConversationFocusProduct: {FocusProductName} | Price: {FocusProductPrice:N0} VND");

            foreach (var f in Facts)
                lines.Add($"{f.Key}: {f.Value}");

            foreach (var p in Products)
                lines.Add($"Product: {p.Name} | {p.Price:N0} VND | Category: {p.Category} | Sold: {p.SoldQuantity}");

            if (Revenue != null)
            {
                lines.Add($"RevenueToday: {Revenue.Today:N0} VND");
                lines.Add($"RevenueWeek: {Revenue.Week:N0} VND");
                lines.Add($"RevenueMonth: {Revenue.Month:N0} VND");
                lines.Add($"RevenueYear: {Revenue.Year:N0} VND");
                lines.Add($"FilteredRevenue: {Revenue.Filtered:N0} VND ({Revenue.FilterLabel})");
            }

            if (Orders != null)
            {
                lines.Add($"TotalConfirmedOrders: {Orders.TotalConfirmed}");
                lines.Add($"AverageOrderValue: {Orders.AverageOrderValue:N0} VND");
                lines.Add($"PendingOrders: {Orders.Pending}");
                if (!string.IsNullOrWhiteSpace(Orders.StatusBreakdown))
                    lines.Add($"OrderStatus: {Orders.StatusBreakdown}");
            }

            foreach (var i in LowInventory)
                lines.Add($"LowStock: {i.Name} = {i.Quantity} {i.Unit}");

            if (Cart != null)
                lines.Add($"CustomerCart: {Cart.ItemCount} items, total {Cart.Total:N0} VND");

            return string.Join(Environment.NewLine, lines);
        }
    }

    public class ContextFact
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ProductFactDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int SoldQuantity { get; set; }
    }

    public class RevenueFactDto
    {
        public decimal Today { get; set; }
        public decimal Week { get; set; }
        public decimal Month { get; set; }
        public decimal Year { get; set; }
        public decimal Filtered { get; set; }
        public string FilterLabel { get; set; } = string.Empty;
    }

    public class OrderFactDto
    {
        public int TotalConfirmed { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int Pending { get; set; }
        public string? StatusBreakdown { get; set; }
        public int CakesSoldToday { get; set; }
    }

    public class InventoryFactDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class CartFactDto
    {
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
    }
}
