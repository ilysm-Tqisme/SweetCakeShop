using Microsoft.AspNetCore.Identity;
namespace SweetCakeShop.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        // If the order was placed by an authenticated user, this will be populated.
        // For guest checkout this stays empty.
        public string UserId { get; set; } = string.Empty;

        // Shipping / customer fields (for guest checkout and for records)
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public bool IsGuest { get; set; } = true;

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? ConfirmedAt { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Pending";     // Pending, Confirmed, Shipped, Delivered, Cancelled

        // Coupon / discount tracking
        public int? CouponId { get; set; }
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }

        public IdentityUser? User { get; set; }           // nếu dùng Identity
        public Coupon? Coupon { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
