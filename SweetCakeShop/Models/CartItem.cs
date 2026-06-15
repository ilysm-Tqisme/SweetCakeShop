namespace SweetCakeShop.Models
{
    /// <summary>
    /// Persistent, database-backed cart item that survives session expiry
    /// and enables multi-device sync for authenticated users.
    /// </summary>
    public class CartItem
    {
        public int CartItemId { get; set; }

        /// <summary>IdentityUser.Id — only authenticated users get DB-persisted carts.</summary>
        public string UserId { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public int Quantity { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Product? Product { get; set; }
    }
}
