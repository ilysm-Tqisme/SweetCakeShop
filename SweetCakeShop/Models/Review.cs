using Microsoft.AspNetCore.Identity;

namespace SweetCakeShop.Models
{
    /// <summary>
    /// Product review with 1-5 star rating. Enforces one review per user per product
    /// via a unique composite index on (UserId, ProductId).
    /// </summary>
    public class Review
    {
        public int ReviewId { get; set; }

        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }

        /// <summary>1–5 star rating.</summary>
        public int Rating { get; set; }

        public string? Title { get; set; }
        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Admin moderation flag. Reviews are auto-approved by default.</summary>
        public bool IsApproved { get; set; } = true;

        // Navigation
        public IdentityUser? User { get; set; }
        public Product? Product { get; set; }
    }
}
