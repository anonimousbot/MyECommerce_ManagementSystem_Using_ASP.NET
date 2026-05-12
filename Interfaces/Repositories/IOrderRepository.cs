using EMS.Models.DTOs;
using EMS.Models.DTOs.Orders;
using EMS.Models.Entities;

namespace EMS.Interfaces.Repositories
{
    public interface IOrderRepository : IBaseRepository
    {
        Task<int> GetOrderCounts();
        Task<Order> GetOrderById(Guid id);
        Task<Order?> GetOrderByPaymentReferenceAsync(string paymentReference);
        IQueryable<Order> QueryAllOrders();
        IQueryable<Order> QueryOrdersByCustomer(Guid customerId);
        Task<IEnumerable<Order>> GetOrdersByCustomerAsync(Guid customerId);
        Task<IEnumerable<Order>> GetAllOrders();
        Task<IReadOnlyList<Order>> GetPendingOrderAsync();
        Task<IReadOnlyList<Order>> GetProcessingOrderAsync();
        Task<IReadOnlyList<Order>> GetDeliveredOrderAsync();
        Task<IReadOnlyList<Order>> GetCancelledOrderAsync();
    }
}
