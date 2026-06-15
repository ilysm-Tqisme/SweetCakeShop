using SweetCakeShop.Constants;

namespace SweetCakeShop.Models.ViewModels
{
    public class RevenueDashboardViewModel
    {
        public RevenueDateFilter ActiveFilter { get; set; }
        public DateTime RangeStart { get; set; }
        public DateTime RangeEnd { get; set; }
        public string FilterLabel { get; set; } = string.Empty;

        public decimal RevenueToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisYear { get; set; }
        public decimal FilteredRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalConfirmedOrders { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<DashboardOrderItem> RecentOrders { get; set; } = [];
        public List<DashboardTopProductItem> TopSellingProducts { get; set; } = [];

        public ChartDataViewModel Charts { get; set; } = new();
    }

    public class DashboardOrderItem
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class DashboardTopProductItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SoldQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ChartDataViewModel
    {
        public List<string> RevenueLineLabels { get; set; } = [];
        public List<decimal> RevenueLineValues { get; set; } = [];
        public List<string> MonthlyBarLabels { get; set; } = [];
        public List<decimal> MonthlyBarValues { get; set; } = [];
        public List<string> OrderStatusLabels { get; set; } = [];
        public List<int> OrderStatusValues { get; set; } = [];
        public List<string> TopProductLabels { get; set; } = [];
        public List<decimal> TopProductValues { get; set; } = [];
    }
}
