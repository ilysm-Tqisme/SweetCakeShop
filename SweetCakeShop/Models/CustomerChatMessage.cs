namespace SweetCakeShop.Models
{
    public class CustomerChatMessage
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string? ChatToken { get; set; }
        /// <summary>user | model</summary>
        public string Sender { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? ContextProductId { get; set; }
    }
}
