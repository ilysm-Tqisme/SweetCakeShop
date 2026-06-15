namespace SweetCakeShop.Models
{
    /// <summary>
    /// Tracks each use of a coupon — used for per-user limit enforcement
    /// and audit trail.
    /// </summary>
    public class CouponUsage
    {
        public int CouponUsageId { get; set; }

        public int CouponId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int OrderId { get; set; }

        /// <summary>Actual discount amount applied to this order.</summary>
        public decimal DiscountApplied { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.Now;

        // Navigation
        public Coupon? Coupon { get; set; }
        public Order? Order { get; set; }
    }
}
