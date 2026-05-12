using EMS.Interfaces.Repositories;
using EMS.Models.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Respositories
{
    public class OrderRepository : BaseRepository, IOrderRepository
    {

        public OrderRepository(EmsContext emscontext) : base(emscontext)
        {

        }

        public async Task<IReadOnlyList<Order>> GetCancelledOrderAsync()
        {
            return await _emsContext.Set<Order>()
             .OrderByDescending(d => d.DateCreated)
             .Include(o => o.OrderItem)
             .ThenInclude(i => i.Item)
             .Include(c => c.Customer)
             .Where(ap => ap.OrderStatus == Models.Enums.Status.Cancelled)
             .AsNoTracking()
             .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetDeliveredOrderAsync()
        {
            return await _emsContext.Set<Order>()
            .OrderByDescending(d => d.DateCreated)
             .Include(o => o.OrderItem)
             .ThenInclude(i => i.Item)
            .Include(c => c.Customer)
            .Where(ap => ap.OrderStatus == Models.Enums.Status.Delivered)
            .AsNoTracking()
            .ToListAsync();
        }

        public async Task<Order> GetOrderById(Guid id)
        {
            return await _emsContext.Set<Order>()
                .Include(o => o.Customer)
                .Include(o => o.OrderItem)
                .ThenInclude(o => o.Item)
               //.AsNoTracking()
               .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Order?> GetOrderByPaymentReferenceAsync(string paymentReference)
        {
            return await _emsContext.Set<Order>()
                .Include(o => o.Customer)
                .Include(o => o.OrderItem)
                .ThenInclude(oi => oi.Item)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.PaymentReference == paymentReference);
        }

        public async Task<int> GetOrderCounts()
        {
            return await _emsContext.Set<Order>()
                 .CountAsync();
        }

        public IQueryable<Order> QueryAllOrders()
        {
            return _emsContext.Set<Order>()
                .Include(x => x.OrderItem)
                .ThenInclude(x => x.Item)
                .Include(x => x.Customer)
                .AsNoTracking();
        }

        public IQueryable<Order> QueryOrdersByCustomer(Guid customerId)
        {
            return _emsContext.Set<Order>()
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.OrderItem)
                .ThenInclude(i => i.Item)
                .Include(o => o.Customer)
                .AsNoTracking();
        }

        public async Task<IEnumerable<Order>> GetAllOrders()
        {
            return await QueryAllOrders().ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerAsync(Guid customerId)
        {
            return await QueryOrdersByCustomer(customerId).ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetPendingOrderAsync()
        {
            return await _emsContext.Set<Order>()
            .OrderByDescending(d => d.DateCreated)
            .Include(o => o.OrderItem)
             .ThenInclude(i => i.Item)
            .Include(c => c.Customer)
            .Where(ap => ap.OrderStatus == Models.Enums.Status.Pending)
            .AsNoTracking()
            .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetProcessingOrderAsync()
        {
            return await _emsContext.Set<Order>()
            .OrderByDescending(d => d.DateCreated)
            .Include(o => o.OrderItem)
            .ThenInclude(i => i.Item)
            .Include(c => c.Customer)
            .Where(ap => ap.OrderStatus == Models.Enums.Status.Processing)
            .AsNoTracking()
            .ToListAsync();
        }
    }
}
