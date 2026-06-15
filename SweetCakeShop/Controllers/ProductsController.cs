using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;
using SweetCakeShop.Services;
using X.PagedList;
using X.PagedList.Extensions;
using System.Security.Claims;

namespace SweetCakeShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartSearchService _smartSearch;
        private readonly IReviewService _reviewService;

        public ProductsController(ApplicationDbContext context, ISmartSearchService smartSearch, IReviewService reviewService)
        {
            _context = context;
            _smartSearch = smartSearch;
            _reviewService = reviewService;
        }

        // GET: Products
        public async Task<IActionResult> Index(
            string? sortOrder, 
            string? searchTerm, 
            int? page, 
            int? categoryId, 
            string? categoryName,
            decimal? minPrice,
            decimal? maxPrice,
            int? minRating)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentCategoryId"] = categoryId;
            ViewData["CurrentCategoryName"] = categoryName;
            ViewData["MinPrice"] = minPrice;
            ViewData["MaxPrice"] = maxPrice;
            ViewData["MinRating"] = minRating;

            List<Product> filteredProducts;

            // 1. Smart Search (if provided)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchResults = await _smartSearch.SearchAsync(searchTerm);
                filteredProducts = searchResults.Select(r => r.Product).ToList();
            }
            else
            {
                filteredProducts = await _context.Products.Include(p => p.Category).ToListAsync();
            }

            // 2. Advanced Filtering
            var query = filteredProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var categoryKeyword = categoryName.Trim();
                query = query.Where(p => p.Category != null && p.Category.CategoryName == categoryKeyword);
            }
            else if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // If rating filter applied, fetch average ratings and filter
            if (minRating.HasValue && minRating.Value > 0)
            {
                var productIds = query.Select(p => p.ProductId).ToList();
                var ratings = await _reviewService.GetAverageRatingsAsync(productIds);
                query = query.Where(p => ratings.ContainsKey(p.ProductId) && ratings[p.ProductId] >= minRating.Value);
            }

            // 3. Sorting
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderBy(p => p.ProductId) // Default
            };

            int pageSize = 12;
            int pageNumber = page ?? 1;
            var pagedProducts = query.ToPagedList(pageNumber, pageSize);
            
            return View("IndexPro", pagedProducts);
        }

        [HttpGet]
        public async Task<IActionResult> IndexPro(
            string? sortOrder, 
            string? searchTerm, 
            int? page, 
            int? categoryId, 
            string? categoryName,
            decimal? minPrice,
            decimal? maxPrice,
            int? minRating)
        {
            return await Index(sortOrder, searchTerm, page, categoryId, categoryName, minPrice, maxPrice, minRating);
        }

        [HttpGet]
        public async Task<IActionResult> Autocomplete(string term)
        {
            var suggestions = await _smartSearch.AutocompleteAsync(term);
            return Json(suggestions);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var similarProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .OrderBy(p => p.ProductId)
                .Take(6)
                .ToListAsync();

            var reviews = await _reviewService.GetProductReviewsAsync(product.ProductId);
            var averageRating = await _reviewService.GetAverageRatingAsync(product.ProductId);

            string? userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            bool hasUserReviewed = userId != null && await _reviewService.HasUserReviewedAsync(userId, product.ProductId);

            var reviewDisplays = reviews.Select(r => new ReviewDisplayViewModel
            {
                ReviewId = r.ReviewId,
                UserName = r.User?.UserName ?? "Anonymous",
                Rating = r.Rating,
                Title = r.Title,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                IsOwnReview = userId != null && r.UserId == userId
            }).ToList();

            var model = new ProductDetailsViewModel
            {
                Product = product,
                SimilarProducts = similarProducts,
                Reviews = reviewDisplays,
                AverageRating = averageRating,
                TotalReviews = reviews.Count,
                HasUserReviewed = hasUserReviewed
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(ProductReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin đánh giá.";
                return RedirectToAction(nameof(Details), new { id = model.ProductId });
            }

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                await _reviewService.CreateReviewAsync(userId, model.ProductId, model.Rating, model.Title, model.Content);
                TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi lưu đánh giá. Vui lòng thử lại sau.";
            }

            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(ProductReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin đánh giá.";
                return RedirectToAction(nameof(Details), new { id = model.ProductId });
            }

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var result = await _reviewService.UpdateReviewAsync(userId, model.ReviewId, model.Rating, model.Title, model.Content);
                if (result != null)
                {
                    TempData["Success"] = "Đã cập nhật đánh giá thành công!";
                }
                else
                {
                    TempData["Error"] = "Không thể cập nhật đánh giá (hoặc bạn không có quyền).";
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật đánh giá. Vui lòng thử lại sau.";
            }

            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId, int productId)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var success = await _reviewService.DeleteReviewAsync(userId, reviewId);

            if (success)
            {
                TempData["Success"] = "Đã xóa đánh giá của bạn.";
            }
            else
            {
                TempData["Error"] = "Không thể xóa đánh giá (hoặc bạn không có quyền).";
            }

            return RedirectToAction(nameof(Details), new { id = productId });
        }
    }
}
