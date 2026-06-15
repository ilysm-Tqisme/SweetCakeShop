using SweetCakeShop.Models;

namespace SweetCakeShop.Models.ViewModels
{
    /// <summary>ViewModel for the product details page including reviews.</summary>
    public class ProductReviewViewModel
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; } = 5;
        public string? Title { get; set; }
        public string? Content { get; set; }
    }

    /// <summary>ViewModel for displaying a review on the product details page.</summary>
    public class ReviewDisplayViewModel
    {
        public int ReviewId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOwnReview { get; set; }
    }
}
