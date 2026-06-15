namespace SweetCakeShop.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal TotalAmount => Items.Sum(i => i.Subtotal);
        
        /// <summary>Applied coupon code, if any.</summary>
        public string? CouponCode { get; set; }

        /// <summary>Discount amount from applied coupon.</summary>
        public decimal DiscountAmount { get; set; }
    }
}
