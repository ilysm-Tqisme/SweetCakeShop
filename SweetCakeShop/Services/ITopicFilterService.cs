namespace SweetCakeShop.Services
{
    public interface ITopicFilterService
    {
        bool IsClearlyOffTopic(string? message);
        string GetRejectionMessage(string languageCode);
    }
}
