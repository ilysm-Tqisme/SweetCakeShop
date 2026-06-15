using System.ComponentModel.DataAnnotations;

namespace SweetCakeShop.Models.Api
{
    public class ChatApiRequest
    {
        [Required(ErrorMessage = "Message is required.")]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>VD: /Products/Details/3 — để bot biết trang hiện tại.</summary>
        [MaxLength(500)]
        public string? PageUrl { get; set; }

        public int? ProductId { get; set; }
    }
}
