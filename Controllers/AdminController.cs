using EMS.Interfaces.Repositories;
using EMS.Models.DTOs.Admin;
using EMS.Models.Entities;
using EMS.Models.Enums;
using EMS.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMS.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class AdminController(
        IItemRepository itemRepository,
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IPaymentRepository paymentRepository) : Controller
    {
        private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        private readonly IOrderRepository _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        private readonly ICustomerRepository _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        private readonly IPaymentRepository _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var salesWindowStart = today.AddDays(-6);
            var previousSalesWindowStart = salesWindowStart.AddDays(-7);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var items = await _itemRepository.Query<Item>()
                .AsNoTracking()
                .OrderBy(i => i.Name)
                .ToListAsync(cancellationToken);

            var inventoryMax = items.Count == 0 ? 1 : Math.Max(1, items.Max(i => i.QuantityInStock));
            var lowStockAlerts = items.Count(i => i.QuantityInStock <= 5);

            var successfulPayments = (await _paymentRepository.Query<Payment>()
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Successful &&
                            (p.DateCreated >= previousSalesWindowStart || (p.PaidAt != null && p.PaidAt >= previousSalesWindowStart)))
                .Select(p => new
                {
                    p.Amount,
                    p.DateCreated,
                    p.PaidAt
                })
                .ToListAsync(cancellationToken))
                .Select(p => new PaymentSnapshot(p.Amount, p.DateCreated, p.PaidAt))
                .ToList();

            var totalSales = await _paymentRepository.Query<Payment>()
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Successful)
                .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

            var salesTrend = BuildSalesTrend(successfulPayments, salesWindowStart);

            var currentWindowSales = successfulPayments
                .Where(p => EffectiveDate(p.PaidAt, p.DateCreated) >= salesWindowStart)
                .Sum(p => p.Amount);

            var previousWindowSales = successfulPayments
                .Where(p =>
                {
                    var effectiveDate = EffectiveDate(p.PaidAt, p.DateCreated);
                    return effectiveDate >= previousSalesWindowStart && effectiveDate < salesWindowStart;
                })
                .Sum(p => p.Amount);

            var salesChangePercentage = previousWindowSales <= 0m
                ? (currentWindowSales > 0m ? 100m : 0m)
                : ((currentWindowSales - previousWindowSales) / previousWindowSales) * 100m;

            var pendingOrders = await _orderRepository.Query<Order>()
                .AsNoTracking()
                .CountAsync(o => o.OrderStatus == Status.Pending, cancellationToken);

            var pendingOrdersToday = await _orderRepository.Query<Order>()
                .AsNoTracking()
                .CountAsync(o => o.OrderStatus == Status.Pending && o.DateCreated >= today, cancellationToken);

            var totalCustomers = await _customerRepository.Query<Customer>()
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var newCustomersThisMonth = await _customerRepository.Query<Customer>()
                .AsNoTracking()
                .CountAsync(c => c.DateCreated >= monthStart, cancellationToken);

            var recentOrders = await _orderRepository.QueryAllOrders()
                .OrderByDescending(o => o.DateCreated)
                .Take(4)
                .ToListAsync(cancellationToken);

            var inventoryStatus = items
                .OrderBy(i => i.QuantityInStock <= 5 ? 0 : 1)
                .ThenBy(i => i.QuantityInStock)
                .ThenBy(i => i.Name)
                .Take(4)
                .Select(i => new AdminInventoryStatusViewModel
                {
                    Label = i.Name,
                    QuantityInStock = i.QuantityInStock,
                    Percentage = (int)Math.Round((double)i.QuantityInStock / inventoryMax * 100d),
                    IsCritical = i.QuantityInStock <= 5
                })
                .ToList();

            var recommendation = lowStockAlerts > 0
                ? $"Restock {items.Where(i => i.QuantityInStock <= 5).OrderBy(i => i.QuantityInStock).First().Name} soon to avoid fulfilment delays."
                : "Stock health looks stable across the catalog.";

            var model = new AdminDashboardViewModel
            {
                TotalSales = totalSales,
                SalesChangePercentage = Math.Round(salesChangePercentage, 1),
                PendingOrders = pendingOrders,
                PendingOrdersToday = pendingOrdersToday,
                LowStockAlerts = lowStockAlerts,
                NewCustomersThisMonth = newCustomersThisMonth,
                TotalCustomers = totalCustomers,
                TotalProducts = items.Count,
                TotalUnitsInStock = items.Sum(i => i.QuantityInStock),
                InventoryValue = items.Sum(i => i.Price * i.QuantityInStock),
                InventoryRecommendation = recommendation,
                SalesTrend = salesTrend,
                InventoryStatus = inventoryStatus,
                RecentOrders = recentOrders.Select(order => new AdminRecentOrderViewModel
                {
                    OrderId = order.Id,
                    OrderNumber = $"#ORD-{order.Id.ToString("N")[..6].ToUpperInvariant()}",
                    CustomerName = order.Customer?.FullName() ?? "Unknown customer",
                    ProductSummary = SummarizeProducts(order),
                    Status = order.OrderStatus,
                    Amount = order.Amount,
                    CreatedAt = order.DateCreated
                }).ToList()
            };

            return View(model);
        }

        private static DateTime EffectiveDate(DateTime? paidAt, DateTime fallbackDate)
        {
            return (paidAt ?? fallbackDate).Date;
        }

        private static List<AdminSalesTrendPointViewModel> BuildSalesTrend(IEnumerable<PaymentSnapshot> payments, DateTime salesWindowStart)
        {
            var paymentList = payments
                .Select(p => new
                {
                    Date = EffectiveDate(p.PaidAt, p.DateCreated),
                    Amount = (decimal)p.Amount
                })
                .ToList();

            var points = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var day = salesWindowStart.AddDays(offset);
                    var amount = paymentList
                        .Where(p => p.Date == day)
                        .Sum(p => p.Amount);

                    return new AdminSalesTrendPointViewModel
                    {
                        Label = day.ToString("ddd"),
                        Amount = amount,
                        IsCurrent = day == DateTime.UtcNow.Date
                    };
                })
                .ToList();

            var maxAmount = points.Count == 0 ? 0m : points.Max(p => p.Amount);
            foreach (var point in points)
            {
                point.Percentage = maxAmount <= 0m
                    ? 18
                    : Math.Max(18, (int)Math.Round(point.Amount / maxAmount * 100m));
            }

            return points;
        }

        private sealed record PaymentSnapshot(decimal Amount, DateTime DateCreated, DateTime? PaidAt);

        private static string SummarizeProducts(Order order)
        {
            var items = order.OrderItem?.Where(i => i.Item != null).ToList() ?? [];
            if (items.Count == 0)
            {
                return "No item details";
            }

            if (items.Count == 1)
            {
                return items[0].Item.Name;
            }

            return $"{items[0].Item.Name} +{items.Count - 1} more";
        }
    }
}
