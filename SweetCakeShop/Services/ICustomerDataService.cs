namespace SweetCakeShop.Services
{
    public interface ICustomerDataService
    {
        Task<string> BuildCustomerContextAsync(CancellationToken cancellationToken = default);
    }
}
