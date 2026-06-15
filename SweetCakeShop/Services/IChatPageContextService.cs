namespace SweetCakeShop.Services
{
    public interface IChatPageContextService
    {
        string? GetCurrentPageContext();
        string? ResolvePageContext(string? pageUrl, int? productId);
    }
}
