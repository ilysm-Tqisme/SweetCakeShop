namespace SweetCakeShop.Models.Api
{
    public class ChatProductCardDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
        public string DetailUrl { get; set; } = string.Empty;
    }
}
