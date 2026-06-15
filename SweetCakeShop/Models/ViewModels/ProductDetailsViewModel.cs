using System.Collections.Generic;

namespace SweetCakeShop.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> SimilarProducts { get; set; } = new();

        // Review properties
        public List<ReviewDisplayViewModel> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool HasUserReviewed { get; set; }
    }
}
