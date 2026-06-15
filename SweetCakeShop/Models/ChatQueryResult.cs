using System.Text;
using System.Text.Json;
using SweetCakeShop.Constants;

namespace SweetCakeShop.Models
{
    public class ChatQueryResult
    {
        public ChatQueryAction Action { get; set; }
        public bool HasData { get; set; }
        public string UserQuestion { get; set; } = string.Empty;
        public Dictionary<string, object?> Facts { get; set; } = new();

        public string ToSystemContextBlock(bool forAdmin)
        {
            if (!HasData)
            {
                return forAdmin
                    ? "Status: No accurate business data available for this query/time range."
                    : "Status: Product or store data unavailable for this query. Suggest browsing Products page or hotline 1900-SWEET.";
            }

            var sb = new StringBuilder();
            MapFactsToContextLines(sb);
            return sb.ToString().TrimEnd();
        }

        private void MapFactsToContextLines(StringBuilder sb)
        {
            if (Facts.TryGetValue("ten_banh", out var name) && Facts.TryGetValue("gia", out var price))
            {
                sb.AppendLine($"Highest Price Product: {name}");
                sb.AppendLine($"Price: {price}");
                if (Facts.TryGetValue("danh_muc", out var cat))
                    sb.AppendLine($"Category: {cat}");
            }

            if (Facts.TryGetValue("ban_chay_nhat", out var top))
                sb.AppendLine($"Top Selling Product: {top}");

            foreach (var kv in Facts.Where(f => f.Key.StartsWith("top_")).OrderBy(f => f.Key))
                sb.AppendLine($"Top Seller: {kv.Value}");

            if (Facts.TryGetValue("doanh_thu_hom_nay", out var today))
                sb.AppendLine($"Today's Revenue: {today}");
            if (Facts.TryGetValue("doanh_thu_loc", out var filtered))
                sb.AppendLine($"Filtered Revenue: {filtered}");
            if (Facts.TryGetValue("bo_loc", out var label))
                sb.AppendLine($"Period: {label}");
            if (Facts.TryGetValue("doanh_thu_tuan", out var week))
                sb.AppendLine($"Week Revenue: {week}");
            if (Facts.TryGetValue("doanh_thu_thang", out var month))
                sb.AppendLine($"Month Revenue: {month}");
            if (Facts.TryGetValue("doanh_thu_nam", out var year))
                sb.AppendLine($"Year Revenue: {year}");
            if (Facts.TryGetValue("don_xac_nhan", out var orders))
                sb.AppendLine($"Confirmed Orders: {orders}");
            if (Facts.TryGetValue("don_cho", out var pending))
                sb.AppendLine($"Pending Orders: {pending}");
            if (Facts.TryGetValue("so_banh_ban_hom_nay", out var cakes))
                sb.AppendLine($"Cakes Sold Today (qty): {cakes}");
            if (Facts.TryGetValue("gia_tri_tb_don", out var aov))
                sb.AppendLine($"Average Order Value: {aov}");

            if (Facts.TryGetValue("giao_hang", out var ship))
            {
                sb.AppendLine($"Delivery: {ship}");
                if (Facts.TryGetValue("thoi_gian", out var time))
                    sb.AppendLine($"Delivery Time: {time}");
            }

            foreach (var kv in Facts.Where(f => f.Key.StartsWith("sp_") || f.Key.StartsWith("goi_y_")).OrderBy(f => f.Key))
                sb.AppendLine($"Product: {kv.Value}");

            foreach (var kv in Facts.Where(f => f.Key.StartsWith("ton_kho_")).OrderBy(f => f.Key))
                sb.AppendLine($"Low Inventory: {kv.Value}");

            foreach (var kv in Facts.Where(f => f.Key.StartsWith("trang_thai_")))
                sb.AppendLine($"Order Status {kv.Key.Replace("trang_thai_", "")}: {kv.Value}");

            var mapped = new HashSet<string>(StringComparer.Ordinal)
            {
                "ten_banh", "gia", "danh_muc", "mo_ta", "ban_chay_nhat", "doanh_thu_hom_nay", "doanh_thu_loc",
                "bo_loc", "doanh_thu_tuan", "doanh_thu_thang", "doanh_thu_nam", "don_xac_nhan", "don_cho",
                "so_banh_ban_hom_nay", "gia_tri_tb_don", "giao_hang", "thoi_gian", "ho_tro", "cach_dat", "thanh_toan",
                "khuyen_mai", "lien_he", "hotline", "email"
            };

            foreach (var kv in Facts.Where(f => !mapped.Contains(f.Key) && !f.Key.StartsWith("top_") && !f.Key.StartsWith("sp_")
                         && !f.Key.StartsWith("goi_y_") && !f.Key.StartsWith("ton_kho_") && !f.Key.StartsWith("trang_thai_")
                         && !f.Key.StartsWith("thap_")))
                sb.AppendLine($"{kv.Key}: {kv.Value}");
        }

        public string ToAdminJson() =>
            JsonSerializer.Serialize(new { action = Action.ToString(), hasData = HasData, facts = Facts });

        public string ToPromptBlock() => ToSystemContextBlock(forAdmin: false);
    }
}
