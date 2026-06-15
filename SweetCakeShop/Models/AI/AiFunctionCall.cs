namespace SweetCakeShop.Models.AI
{
    public class AiFunctionCall
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object?> Arguments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class AiFunctionPlan
    {
        public List<AiFunctionCall> Calls { get; set; } = [];
        public string? ReasoningNote { get; set; }
    }
}
