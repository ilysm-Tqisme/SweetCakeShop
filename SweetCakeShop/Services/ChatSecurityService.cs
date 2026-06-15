using System.Text.RegularExpressions;

namespace SweetCakeShop.Services
{
    public class ChatSecurityService : IChatSecurityService
    {
        private static readonly string[] CredentialPatterns =
        [
            @"tài\s*khoản\s*admin", @"tk\s*mk", @"tkmk", @"mật\s*khẩu\s*admin", @"password\s*admin",
            @"admin\s*password", @"admin\s*account", @"cho\s*tôi\s*tk", @"cho\s*tk", @"cấp\s*quyền\s*admin",
            @"đăng\s*nhập\s*admin", @"login\s*admin", @"api\s*key", @"secret\s*key", @"connection\s*string",
            @"sql\s*server\s*password", @"hack", @"bypass", @"exploit"
        ];

        public bool IsRestrictedRequest(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            var n = Regex.Replace(message.Trim().ToLowerInvariant(), @"\s+", " ");
            return CredentialPatterns.Any(p => Regex.IsMatch(n, p, RegexOptions.IgnoreCase));
        }

        public string GetStaffRejectionMessage(string languageCode, AiChatMode mode) =>
            languageCode == "en"
                ? "I'm sorry — as a SweetCakeShop consultant I can't help with passwords, admin accounts, or system credentials. I can help you with cakes, orders, delivery, or (if you're staff) business reports on the dashboard."
                : mode == AiChatMode.Admin
                    ? "Dạ, em là trợ lý phân tích kinh doanh — không thể cung cấp mật khẩu, tài khoản hệ thống hay thông tin bảo mật. Anh/chị hỏi doanh thu, đơn hàng, tồn kho, sản phẩm bán chạy em hỗ trợ ngay ạ."
                    : "Dạ, em là nhân viên tư vấn bánh SweetCakeShop — không nằm trong phạm vi hỗ trợ mật khẩu hay tài khoản quản trị ạ. Anh/chị cần gợi ý bánh, giá, đặt hàng, giao hàng thì em tư vấn liền cho ạ!";
    }
}
