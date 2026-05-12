using EMS.Models.Enums;

namespace EMS.Models.DTOs.Admin
{
    public class AdminDashboardViewModel
    {
        public decimal TotalSales { get; set; }
        public decimal SalesChangePercentage { get; set; }
        public int PendingOrders { get; set; }
        public int PendingOrdersToday { get; set; }
        public int LowStockAlerts { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalUnitsInStock { get; set; }
        public decimal InventoryValue { get; set; }
        public string InventoryRecommendation { get; set; } = string.Empty;
        public IReadOnlyList<AdminSalesTrendPointViewModel> SalesTrend { get; set; } = [];
        public IReadOnlyList<AdminInventoryStatusViewModel> InventoryStatus { get; set; } = [];
        public IReadOnlyList<AdminRecentOrderViewModel> RecentOrders { get; set; } = [];
    }

    public class AdminSalesTrendPointViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Percentage { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class AdminInventoryStatusViewModel
    {
        public string Label { get; set; } = string.Empty;
        public int QuantityInStock { get; set; }
        public int Percentage { get; set; }
        public bool IsCritical { get; set; }
    }

    public class AdminRecentOrderViewModel
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductSummary { get; set; } = string.Empty;
        public Status Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
