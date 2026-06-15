namespace SweetCakeShop.Models.ViewModels
{
    public class RevenueStatisticsViewModel
    {
        public decimal TodayRevenue { get; set; }
        public decimal WeeklyRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal YearlyRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
    }
}
