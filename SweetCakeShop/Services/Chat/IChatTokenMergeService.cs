namespace SweetCakeShop.Services.Chat
{
    public interface IChatTokenMergeService
    {
        Task TryMergeOnAuthenticatedRequestAsync(CancellationToken ct = default);
        Task MergeOnLoginAsync(string userId, CancellationToken ct = default);
    }
}
