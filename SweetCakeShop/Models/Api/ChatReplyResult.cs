namespace SweetCakeShop.Models.Api
{
    public class ChatReplyResult
    {
        public string Reply { get; set; } = string.Empty;
        public List<ChatProductCardDto> Products { get; set; } = [];
        public List<string> QuickReplies { get; set; } = [];
    }
}
