namespace SweetCakeShop.Models.Api
{
    public class ChatMessageDto
    {
        public string Sender { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ChatProductCardDto> Products { get; set; } = [];
    }

    public class ChatHistoryResponse
    {
        public bool Success { get; set; } = true;
        public List<ChatMessageDto> Messages { get; set; } = [];
        public List<string> QuickReplies { get; set; } = [];
    }

    public class SendChatMessageRequest
    {
        public string UserMessage { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public string? PageUrl { get; set; }
    }

    public class SendChatMessageResponse
    {
        public bool Success { get; set; }
        public string Reply { get; set; } = string.Empty;
        public List<ChatProductCardDto> Products { get; set; } = [];
        public List<string> QuickReplies { get; set; } = [];
    }
}
