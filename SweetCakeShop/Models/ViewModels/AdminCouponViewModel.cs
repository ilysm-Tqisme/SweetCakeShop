namespace SweetCakeShop.Models.ViewModels
{
    /// <summary>ViewModel for coupon management in admin panel.</summary>
    public class AdminCouponViewModel
    {
        public int CouponId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public decimal MaxDiscountAmount { get; set; }
        public DateTime ValidFrom { get; set; } = DateTime.Now;
        public DateTime ValidTo { get; set; } = DateTime.Now.AddDays(30);
        public int MaxUsageCount { get; set; }
        public int MaxUsagePerUser { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public CouponScope Scope { get; set; } = CouponScope.Global;
        public int? ScopeCategoryId { get; set; }
        public int? ScopeProductId { get; set; }
    }
}
