using System.ComponentModel.DataAnnotations;

namespace SweetCakeShop.Models
{
    public class AIChatRequest
    {
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
