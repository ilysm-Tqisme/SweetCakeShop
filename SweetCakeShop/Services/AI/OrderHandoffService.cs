using System.Text;
using System.Text.RegularExpressions;

namespace SweetCakeShop.Services.AI
{
    /// <summary>Human-in-the-loop: thu thập Tên, SĐT, Địa chỉ, Ngày giờ nhận, Nội dung ghi bánh.</summary>
    public class OrderHandoffService : IOrderHandoffService
    {
        private static readonly string[] OrderIntentPatterns =
        [
            @"đặt hàng", @"dat hang", @"chốt đơn", @"chot don", @"đặt bánh", @"dat banh",
            @"order now", @"place order", @"muốn mua", @"muon mua", @"giao cho"
        ];

        private static readonly string[] HumanAgentTriggers =
        [
            @"gặp nhân viên", @"gap nhan vien", @"gặp người", @"gap nguoi", @"người thật", @"nguoi that",
            @"nhân viên thật", @"nhan vien that", @"talk to human", @"real person", @"gọi shop", @"goi shop",
            @"alo shop", @"khiếu nại", @"khieu nai", @"bồi thường", @"boi thuong", @"tệ quá", @"te qua",
            @"dở quá", @"do qua", @"scam", @"lừa đảo", @"lua dao"
        ];

        public void UpdateFromMessage(ConversationSessionState state, string message)
        {
            var t = message.Trim();
            if (string.IsNullOrWhiteSpace(t)) return;

            var lower = t.ToLowerInvariant();
            if (!state.OrderCapture.Active && OrderIntentPatterns.Any(p => Regex.IsMatch(lower, p, RegexOptions.IgnoreCase)))
                state.OrderCapture.Active = true;

            if (!state.OrderCapture.Active) return;

            var draft = state.OrderCapture;

            var phone = Regex.Match(t, @"(?:0|\+84)[\d\s.\-]{8,14}\d");
            if (phone.Success) draft.Phone = NormalizePhone(phone.Value);

            if (Regex.IsMatch(lower, @"tên|ten |họ tên|ho ten|tôi là|toi la|em là|em la"))
            {
                var name = Regex.Replace(t, @"(?i)(tên|ten|họ tên|ho ten|tôi là|toi la|em là|em la)\s*:?\s*", "").Trim();
                if (name.Length >= 2 && name.Length <= 80) draft.CustomerName = name;
            }

            if (Regex.IsMatch(lower, @"địa chỉ|dia chi|address|giao đến|giao den|ở |o "))
            {
                var addr = Regex.Replace(t, @"(?i)(địa chỉ|dia chi|address|giao đến|giao den)\s*:?\s*", "").Trim();
                if (addr.Length >= 5) draft.Address = addr;
            }

            if (Regex.IsMatch(lower, @"ngày|ngay |giờ|gio |nhận|nhan |giao lúc|giao luc|hôm|hom "))
                draft.DeliveryDateTime ??= t;

            if (Regex.IsMatch(lower, @"ghi (lên|len)|chữ trên|chu tren|nội dung|noi dung|message on cake"))
            {
                var msg = Regex.Replace(t, @"(?i)(ghi (lên|len)|chữ trên|chu tren|nội dung|noi dung)\s*:?\s*", "").Trim();
                if (msg.Length >= 1) draft.CakeMessage = msg;
            }

            draft.ReadyForHumanHandoff = IsComplete(draft);
        }

        public bool RequestsHumanAgent(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return HumanAgentTriggers.Any(p => Regex.IsMatch(lower, p, RegexOptions.IgnoreCase));
        }

        public string GetImmediateHandoffReply(string languageCode = "vi") =>
            languageCode == "en"
                ? "I'll connect you with our bakery staff right away — please call **1900-SWEET** or wait a moment on chat. 🍰"
                : "Dạ, em chuyển anh/chị cho nhân viên tiệm ngay ạ! 🍰 Gọi **1900-SWEET** hoặc đợi vài phút, nhân viên sẽ vào chat hỗ trợ mình nhé.";

        public string BuildRagSupplement(ConversationSessionState state)
        {
            var draft = state.OrderCapture;
            if (!draft.Active) return "";

            var sb = new StringBuilder();
            sb.AppendLine("--- Luồng đặt hàng (human-in-the-loop) ---");
            sb.AppendLine($"Tên: {draft.CustomerName ?? "(chưa có)"}");
            sb.AppendLine($"SĐT: {draft.Phone ?? "(chưa có)"}");
            sb.AppendLine($"Địa chỉ: {draft.Address ?? "(chưa có)"}");
            sb.AppendLine($"Ngày giờ nhận: {draft.DeliveryDateTime ?? "(chưa có)"}");
            sb.AppendLine($"Ghi trên bánh: {draft.CakeMessage ?? "(chưa có)"}");

            if (draft.ReadyForHumanHandoff)
            {
                sb.AppendLine("TRẠNG THÁI: Đủ thông tin — xác nhận ngắn gọn và báo nhân viên tiệm sẽ gọi/xác nhận đơn trong ít phút.");
            }
            else
            {
                var missing = GetMissingFields(draft);
                sb.AppendLine($"Còn thiếu: {string.Join(", ", missing)} — hỏi lịch sự từng mục, tối đa 1 câu.");
            }

            if (state.Focus?.ProductName != null)
                sb.AppendLine($"Bánh đang chọn: {state.Focus.ProductName}");

            return sb.ToString();
        }

        public static bool IsComplete(OrderCaptureDraft d) =>
            !string.IsNullOrWhiteSpace(d.CustomerName)
            && !string.IsNullOrWhiteSpace(d.Phone)
            && !string.IsNullOrWhiteSpace(d.Address)
            && !string.IsNullOrWhiteSpace(d.DeliveryDateTime);

        private static IReadOnlyList<string> GetMissingFields(OrderCaptureDraft d)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(d.CustomerName)) list.Add("Tên");
            if (string.IsNullOrWhiteSpace(d.Phone)) list.Add("SĐT");
            if (string.IsNullOrWhiteSpace(d.Address)) list.Add("Địa chỉ");
            if (string.IsNullOrWhiteSpace(d.DeliveryDateTime)) list.Add("Ngày giờ nhận");
            if (string.IsNullOrWhiteSpace(d.CakeMessage)) list.Add("Nội dung ghi bánh (nếu có)");
            return list;
        }

        private static string NormalizePhone(string raw) =>
            Regex.Replace(raw, @"[^\d+]", "");
    }
}
