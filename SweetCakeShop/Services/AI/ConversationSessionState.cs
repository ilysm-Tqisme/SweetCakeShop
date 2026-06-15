namespace SweetCakeShop.Services.AI
{
    public class ConversationFocus
    {
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal? ProductPrice { get; set; }
        public string? Category { get; set; }
    }

    public class UserConversationPreferences
    {
        public string? Occasion { get; set; }
        public string? FlavorOrStyle { get; set; }
        public decimal? BudgetMax { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>Thông tin đặt bánh thu thập dần — khi đủ sẽ chuyển nhân viên thật.</summary>
    public class OrderCaptureDraft
    {
        public bool Active { get; set; }
        public string? CustomerName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? DeliveryDateTime { get; set; }
        public string? CakeMessage { get; set; }
        public bool ReadyForHumanHandoff { get; set; }
    }

    public class ConversationSessionState
    {
        public ConversationFocus? Focus { get; set; }
        public List<string> RecentProductNames { get; set; } = [];
        public string? LastDiscussedTopic { get; set; }
        public string? LastAssistantReplySnippet { get; set; }
        public string? UserInterest { get; set; }
        public UserConversationPreferences Preferences { get; set; } = new();
        public OrderCaptureDraft OrderCapture { get; set; } = new();
    }
}
