using System.ComponentModel.DataAnnotations;

namespace SweetCakeShop.Models.ViewModels
{
    public class AiChatRequestViewModel
    {
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
