namespace SweetCakeShop.Services
{
    public interface IAiBusinessDataService
    {
        Task<string> BuildAdminContextAsync(CancellationToken cancellationToken = default);
    }
}
