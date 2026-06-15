namespace SweetCakeShop.Models
{
    /// <summary>
    /// In-app notification triggered by system events (add_to_cart, discount_applied,
    /// coupon_created, order_status_changed, etc.).
    /// </summary>
    public class Notification
    {
        public int NotificationId { get; set; }

        /// <summary>IdentityUser.Id — null/empty for anonymous session-based notifications.</summary>
        public string? UserId { get; set; }

        /// <summary>Session ID for anonymous users.</summary>
        public string? SessionId { get; set; }

        /// <summary>Event type key (e.g., "add_to_cart", "discount_applied", "order_confirmed").</summary>
        public string EventType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>Optional JSON payload with extra event data.</summary>
        public string? Data { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
