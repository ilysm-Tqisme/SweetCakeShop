namespace SweetCakeShop.Models.Api
{
    public class ChatApiResponse
    {
        public bool Success { get; set; }
        public string Reply { get; set; } = string.Empty;
        /// <summary>Thẻ sản phẩm gợi ý (kiểu video Laravel + Gemini).</summary>
        public List<ChatProductCardDto> Products { get; set; } = [];
        public List<string> QuickReplies { get; set; } = [];
    }
}
