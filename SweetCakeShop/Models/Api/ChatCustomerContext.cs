namespace SweetCakeShop.Models.Api
{
    /// <summary>Ngữ cảnh trang gửi từ widget (giống Laravel tutorial — biết khách đang xem sản phẩm nào).</summary>
    public class ChatCustomerContext
    {
        public string? PageUrl { get; set; }
        public int? ProductId { get; set; }
    }
}
