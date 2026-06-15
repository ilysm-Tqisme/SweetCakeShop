using SweetCakeShop.Models;

namespace SweetCakeShop.Constants
{
    public static class OrderStatuses
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        /// <summary>EF-translatable statuses that count toward revenue.</summary>
        public static readonly string[] RevenueEligibleStatuses = [Confirmed, Completed];

        private static readonly HashSet<string> RevenueEligible = new(RevenueEligibleStatuses, StringComparer.OrdinalIgnoreCase);

        public static bool CountsForRevenue(string? status) =>
            !string.IsNullOrWhiteSpace(status) && RevenueEligible.Contains(status.Trim());

        public static void ApplyConfirmed(Order order)
        {
            order.Status = Confirmed;
            order.ConfirmedAt = DateTime.Now;
        }
    }
}
