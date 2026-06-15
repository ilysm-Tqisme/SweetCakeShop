using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Models.Api;
using SweetCakeShop.Services.AI;

namespace SweetCakeShop.Services.Chat
{
    public class ProductQueryResult
    {
        public bool UseDirectReply { get; set; }
        public string DirectReply { get; set; } = string.Empty;
        public List<ProductFactDto> Products { get; set; } = [];
        public string FactsBlock { get; set; } = string.Empty;
    }

    public interface IProductIntentResolver
    {
        Task<ProductQueryResult> ResolveAsync(
            string userMessage,
            int? pageProductId,
            IReadOnlyList<CustomerChatMessage> recentHistory,
            CancellationToken ct = default);
    }

    public class ProductIntentResolver : IProductIntentResolver
    {
        private static readonly string[] StopWords =
        [
            "bánh", "banh", "bạn", "ban", "có", "co", "không", "khong", "cho", "tôi", "toi",
            "mình", "minh", "em", "ạ", "a", "nhé", "nhe", "ơi", "oi", "hay", "loại", "loai",
            "món", "mon", "nào", "nao", "gì", "gi", "muốn", "muon", "hỏi", "hoi", "xin",
            "vui", "lòng", "long", "shop", "tiệm", "tiem", "cửa", "cua", "hàng", "hang",
            "sweetcakeshop", "dạ", "da", "ạ", "the", "a", "an", "is", "are", "do", "you", "have"
        ];

        private readonly ApplicationDbContext _db;
        private readonly IProductAnalyticsService _analytics;

        public ProductIntentResolver(ApplicationDbContext db, IProductAnalyticsService analytics)
        {
            _db = db;
            _analytics = analytics;
        }

        public async Task<ProductQueryResult> ResolveAsync(
            string userMessage,
            int? pageProductId,
            IReadOnlyList<CustomerChatMessage> recentHistory,
            CancellationToken ct = default)
        {
            var t = userMessage.Trim().ToLowerInvariant();

            if (Regex.IsMatch(t, @"đắt nhất|dat nhat|expensive|giá cao nhất|gia cao nhat|cao nhất|cao nhat"))
                return await SingleProductReply(await _analytics.GetHighestPriceAsync(ct), "đắt nhất");

            if (Regex.IsMatch(t, @"rẻ nhất|re nhat|cheapest|giá thấp|gia thap|ít tiền|it tien"))
                return await SingleProductReply(await _analytics.GetLowestPriceAsync(ct), "rẻ nhất");

            if (Regex.IsMatch(t, @"bán chạy|ban chay|best sell|phổ biến|pho bien"))
            {
                var top = (await _analytics.GetTopSellingAsync(1, ct)).FirstOrDefault();
                return await SingleProductReply(top, "bán chạy nhất");
            }

            if (Regex.IsMatch(t, @"tương tự|tuong tu|giống|giong|same|like above|như trên|nhu tren"))
            {
                var anchorId = pageProductId ?? await ResolveAnchorProductIdAsync(recentHistory, ct);
                if (anchorId.HasValue)
                {
                    var anchor = await GetByIdAsync(anchorId.Value, ct);
                    if (anchor != null)
                    {
                        var related = await _analytics.GetRelatedProductsAsync(anchor.Name, 3, ct);
                        if (related.Count == 0)
                            return NoMatch("Dạ, em chưa tìm thêm món cùng dòng — anh/chị xem thêm tại Menu nhé ạ!");
                        return MultiProductReply(related.Take(3).ToList(),
                            $"Dạ, các món tương tự **{anchor.Name}** 🎂:");
                    }
                }
                return NoMatch("Dạ, anh/chị muốn bánh tương tự món nào ạ? Cho em tên bánh hoặc mở trang chi tiết bánh đó nhé!");
            }

            if (Regex.IsMatch(t, @"có.*(không|khong)|co.*(khong|không)|bán.*(không|khong)"))
            {
                var phrase = ExtractSearchPhrase(userMessage);
                var found = await SearchByPhraseAsync(phrase, 3, ct);
                if (found.Count == 0)
                    return NoMatch($"Dạ, hiện tiệm em chưa có bánh {phrase} — anh/chị đợi em báo nhân viên tiệm hỗ trợ mình ngay nhé ạ!");
                if (found.Count == 1)
                    return await SingleProductReply(found[0], null, $"Dạ, có ạ! **{found[0].Name}** 🍰");
                return MultiProductReply(found, "Dạ, có ạ:");
            }

            var searchPhrase = ExtractSearchPhrase(userMessage);
            if (!string.IsNullOrWhiteSpace(searchPhrase) && searchPhrase.Length >= 2)
            {
                var hits = await SearchByPhraseAsync(searchPhrase, 4, ct);
                if (hits.Count == 1)
                    return await SingleProductReply(hits[0], null);
                if (hits.Count > 1)
                    return MultiProductReply(hits.Take(3).ToList(), "Dạ, em tìm thấy:");
                return NoMatch($"Dạ, tiệm em chưa có **{searchPhrase}** — anh/chị thử món khác trên Menu hoặc gọi 1900-SWEET nhé ạ!");
            }

            return new ProductQueryResult
            {
                UseDirectReply = false,
                FactsBlock = "Không có truy vấn sản phẩm cụ thể — trả lời ngắn theo danh mục hoặc hỏi lại."
            };
        }

        private async Task<ProductQueryResult> SingleProductReply(
            ProductFactDto? p, string? label, string? prefix = null)
        {
            if (p == null)
                return NoMatch("Dạ, em chưa tra được món trong hệ thống — anh/chị xem Menu trên web nhé ạ!");

            var cat = string.IsNullOrWhiteSpace(p.Category) ? "" : $" ({p.Category})";
            var head = prefix ?? (label != null ? $"Dạ, bánh {label} là" : "Dạ");
            return new ProductQueryResult
            {
                UseDirectReply = true,
                DirectReply = $"{head} **{p.Name}** 🍰 — **{p.Price:N0} VND**{cat} ạ.",
                Products = [p],
                FactsBlock = $"ANSWER_PRODUCT: {p.Name} | {p.Price:N0} VND | {p.Category}"
            };
        }

        private static ProductQueryResult MultiProductReply(List<ProductFactDto> items, string head)
        {
            var lines = items.Select(p =>
            {
                var cat = string.IsNullOrWhiteSpace(p.Category) ? "" : $" ({p.Category})";
                return $"**{p.Name}** 🍰 — {p.Price:N0} VND{cat}";
            });
            return new ProductQueryResult
            {
                UseDirectReply = true,
                DirectReply = $"{head}\n{string.Join("\n", lines)}",
                Products = items,
                FactsBlock = string.Join("\n", items.Select(p => $"PRODUCT: {p.Name} | {p.Price:N0}"))
            };
        }

        private static ProductQueryResult NoMatch(string reply) => new()
        {
            UseDirectReply = true,
            DirectReply = reply,
            Products = []
        };

        private async Task<List<ProductFactDto>> SearchByPhraseAsync(string phrase, int take, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return [];

            var p = phrase.Trim().ToLowerInvariant();
            return await (
                from prod in _db.Products.AsNoTracking()
                join c in _db.Categories.AsNoTracking() on prod.CategoryId equals c.CategoryId
                where prod.ProductName.ToLower().Contains(p)
                      || (prod.Description != null && prod.Description.ToLower().Contains(p))
                orderby prod.ProductName.Length
                select new ProductFactDto
                {
                    ProductId = prod.ProductId,
                    Name = prod.ProductName,
                    Price = prod.Price,
                    Category = c.CategoryName,
                    Description = prod.Description,
                    ImageUrl = prod.Image
                }).Take(take).ToListAsync(ct);
        }

        private static string ExtractSearchPhrase(string message)
        {
            var words = Regex.Split(message.ToLowerInvariant(), @"\s+")
                .Where(w => w.Length > 1 && !StopWords.Contains(w))
                .ToList();
            if (words.Count == 0) return string.Empty;
            return string.Join(" ", words.Take(4));
        }

        private async Task<int?> ResolveAnchorProductIdAsync(
            IReadOnlyList<CustomerChatMessage> history, CancellationToken ct)
        {
            var lastUser = history.LastOrDefault(m => m.Sender == "user" && m.ContextProductId.HasValue);
            if (lastUser?.ContextProductId != null)
                return lastUser.ContextProductId;

            var lastModel = history.LastOrDefault(m => m.Sender == "model");
            if (lastModel == null) return null;

            var names = await _db.Products.AsNoTracking().Select(p => new { p.ProductId, p.ProductName }).ToListAsync(ct);
            foreach (var n in names.OrderByDescending(x => x.ProductName.Length))
            {
                if (lastModel.Content.Contains(n.ProductName, StringComparison.OrdinalIgnoreCase))
                    return n.ProductId;
            }
            return null;
        }

        private async Task<ProductFactDto?> GetByIdAsync(int id, CancellationToken ct) =>
            await (
                from prod in _db.Products.AsNoTracking()
                join c in _db.Categories.AsNoTracking() on prod.CategoryId equals c.CategoryId
                where prod.ProductId == id
                select new ProductFactDto
                {
                    ProductId = prod.ProductId,
                    Name = prod.ProductName,
                    Price = prod.Price,
                    Category = c.CategoryName,
                    Description = prod.Description,
                    ImageUrl = prod.Image
                }).FirstOrDefaultAsync(ct);
    }
}
