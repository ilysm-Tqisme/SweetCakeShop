using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Review service handling CRUD operations, ownership validation,
    /// duplicate prevention, and average rating calculation.
    /// </summary>
    public interface IReviewService
    {
        Task<List<Review>> GetProductReviewsAsync(int productId);
        Task<Review?> GetUserReviewAsync(string userId, int productId);
        Task<double> GetAverageRatingAsync(int productId);
        Task<int> GetReviewCountAsync(int productId);
        Task<Review> CreateReviewAsync(string userId, int productId, int rating, string? title, string? content);
        Task<Review?> UpdateReviewAsync(string userId, int reviewId, int rating, string? title, string? content);
        Task<bool> DeleteReviewAsync(string userId, int reviewId);
        Task<bool> HasUserReviewedAsync(string userId, int productId);
        Task<Dictionary<int, double>> GetAverageRatingsAsync(IEnumerable<int> productIds);
        Task<bool> CanUserReviewAsync(string userId, int productId);
    }

    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetProductReviewsAsync(int productId)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Where(r => r.ProductId == productId && r.IsApproved)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetUserReviewAsync(string userId, int productId)
        {
            return await _context.Reviews
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);
        }

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            var hasReviews = await _context.Reviews
                .AnyAsync(r => r.ProductId == productId && r.IsApproved);

            if (!hasReviews) return 0.0;

            return await _context.Reviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .AverageAsync(r => (double)r.Rating);
        }

        public async Task<int> GetReviewCountAsync(int productId)
        {
            return await _context.Reviews
                .CountAsync(r => r.ProductId == productId && r.IsApproved);
        }

        public async Task<Review> CreateReviewAsync(string userId, int productId, int rating, string? title, string? content)
        {
            // Validate rating range
            rating = Math.Clamp(rating, 1, 5);

            // Anti-abuse: check for existing review
            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

            if (existing != null)
            {
                throw new InvalidOperationException("Bạn đã đánh giá sản phẩm này rồi.");
            }

            // Anti-abuse: must have purchased
            if (!await CanUserReviewAsync(userId, productId))
            {
                throw new InvalidOperationException("Chỉ khách hàng đã mua và nhận sản phẩm mới có thể đánh giá.");
            }

            var review = new Review
            {
                UserId = userId,
                ProductId = productId,
                Rating = rating,
                Title = string.IsNullOrWhiteSpace(title) ? null : System.Text.Encodings.Web.HtmlEncoder.Default.Encode(title.Trim()),
                Content = string.IsNullOrWhiteSpace(content) ? null : System.Text.Encodings.Web.HtmlEncoder.Default.Encode(content.Trim()),
                CreatedAt = DateTime.Now,
                IsApproved = true // Auto-approve as per plan
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<Review?> UpdateReviewAsync(string userId, int reviewId, int rating, string? title, string? content)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);

            if (review == null) return null;

            review.Rating = Math.Clamp(rating, 1, 5);
            review.Title = string.IsNullOrWhiteSpace(title) ? null : System.Text.Encodings.Web.HtmlEncoder.Default.Encode(title.Trim());
            review.Content = string.IsNullOrWhiteSpace(content) ? null : System.Text.Encodings.Web.HtmlEncoder.Default.Encode(content.Trim());
            review.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<bool> DeleteReviewAsync(string userId, int reviewId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);

            if (review == null) return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasUserReviewedAsync(string userId, int productId)
        {
            return await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.ProductId == productId);
        }

        public async Task<Dictionary<int, double>> GetAverageRatingsAsync(IEnumerable<int> productIds)
        {
            var ids = productIds.ToList();
            var ratings = await _context.Reviews
                .Where(r => ids.Contains(r.ProductId) && r.IsApproved)
                .GroupBy(r => r.ProductId)
                .Select(g => new { ProductId = g.Key, Average = g.Average(r => (double)r.Rating) })
                .ToListAsync();

            return ratings.ToDictionary(r => r.ProductId, r => r.Average);
        }

        public async Task<bool> CanUserReviewAsync(string userId, int productId)
        {
            // Must have ordered and received the product to review
            return await _context.OrderDetails
                .Include(od => od.Order)
                .AnyAsync(od => od.Order != null && od.Order.UserId == userId 
                             && od.ProductId == productId 
                             && (od.Order.Status == SweetCakeShop.Constants.OrderStatuses.Completed || od.Order.Status == SweetCakeShop.Constants.OrderStatuses.Delivered));
        }
    }
}
