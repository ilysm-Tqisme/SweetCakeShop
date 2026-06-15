namespace SweetCakeShop.Models.AI
{
    public class RagKnowledgeDocument
    {
        public string PrimaryFunction { get; set; } = string.Empty;
        public string StoreDataBlock { get; set; } = string.Empty;
        public AiBusinessContextDto Context { get; set; } = new();
    }
}
