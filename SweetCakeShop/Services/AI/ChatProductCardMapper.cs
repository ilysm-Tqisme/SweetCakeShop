using SweetCakeShop.Models.AI;
using SweetCakeShop.Models.Api;

namespace SweetCakeShop.Services.AI
{
    public static class ChatProductCardMapper
    {
        public static List<ChatProductCardDto> ToCards(IEnumerable<ProductFactDto> products, int max = 4)
        {
            return products
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .Take(max)
                .Select(p => new ChatProductCardDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Price,
                    Category = string.IsNullOrWhiteSpace(p.Category) ? null : p.Category,
                    ImageUrl = p.ImageUrl,
                    DetailUrl = p.ProductId > 0
                        ? $"/Products/Details/{p.ProductId}"
                        : "/Products/IndexPro"
                })
                .ToList();
        }

        public static List<string> DefaultCustomerQuickReplies() =>
        [
            "Bánh sinh nhật gợi ý?",
            "Bánh rẻ nhất?",
            "Giao hàng mấy ngày?",
            "Muốn đặt hàng"
        ];
    }
}
