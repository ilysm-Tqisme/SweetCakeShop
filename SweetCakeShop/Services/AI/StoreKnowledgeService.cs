using System.Text;

namespace SweetCakeShop.Services.AI
{
    /// <summary>
    /// "Bộ não" tĩnh — chính sách, FAQ, luồng web. AI chỉ được bịa ngoài khối [Dữ liệu cửa hàng] từ EF.
    /// </summary>
    public interface IStoreKnowledgeService
    {
        string GetCoreKnowledgeBlock(AiChatMode mode);
        Task<string> GetLiveCatalogOverviewAsync(int take = 12, CancellationToken ct = default);
    }

    public class StoreKnowledgeService : IStoreKnowledgeService
    {
        private readonly IWebsiteKnowledgeService _website;
        private readonly IProductAnalyticsService _products;

        public StoreKnowledgeService(IWebsiteKnowledgeService website, IProductAnalyticsService products)
        {
            _website = website;
            _products = products;
        }

        public string GetCoreKnowledgeBlock(AiChatMode mode)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== THÔNG TIN CỬA HÀNG (chính sách & quy trình — cố định) ===");
            sb.AppendLine("Tên: SweetCakeShop | Hotline: 1900-SWEET | Liên hệ: /Home/Contact");
            sb.AppendLine("""
                GIAO HÀNG: Nội thành, sau khi admin xác nhận đơn — thường 2–3 ngày làm việc. Bánh kem cần đặt trước (không giao ngay trong 30 phút trừ khi tiệm báo có sẵn).
                THANH TOÁN: COD (xác nhận ngay) hoặc Stripe Online trên trang Checkout.
                ĐẶT HÀNG ONLINE: Sản phẩm → Giỏ (/Cart) → Đăng nhập → Checkout → Thanh toán → /Cart/Success.
                ĐỔI TRẢ: Bánh hỏng/dập — chụp ảnh, gọi hotline trong 2 giờ; tiệm xử lý theo chính sách từng đơn.
                KHUYẾN MÃI: Xem web hoặc hỏi hotline; chatbot không tự giảm giá.
                """);
            sb.AppendLine("FAQ:");
            sb.AppendLine("• Giá trên web là giá chính thức — chỉ báo giá có trong danh sách sản phẩm bên dưới.");
            sb.AppendLine("• Không có món trong menu → báo nhân viên tiệm hỗ trợ, không tự chế tên/giá.");
            sb.AppendLine("• Muốn gặp người thật → gõ \"gặp nhân viên\" hoặc gọi 1900-SWEET.");
            sb.AppendLine(mode == AiChatMode.Admin
                ? _website.GetAdminCapabilities()
                : _website.GetCustomerCapabilities());
            sb.AppendLine("=== HẾT THÔNG TIN CỐ ĐỊNH ===");
            return sb.ToString();
        }

        public async Task<string> GetLiveCatalogOverviewAsync(int take = 12, CancellationToken ct = default)
        {
            var items = await _products.GetCatalogAsync(take, ct);
            if (items.Count == 0)
                return "";

            var sb = new StringBuilder();
            sb.AppendLine($"=== MENU HIỆN CÓ (SQL, {items.Count} món — dùng khi khách hỏi chung) ===");
            foreach (var p in items)
            {
                var cat = string.IsNullOrWhiteSpace(p.Category) ? "" : $" | {p.Category}";
                sb.AppendLine($"• {p.Name} — {p.Price:N0} VND{cat}");
            }
            sb.AppendLine("=== HẾT MENU ===");
            return sb.ToString();
        }
    }
}
