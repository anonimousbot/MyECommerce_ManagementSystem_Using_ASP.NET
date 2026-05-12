using EMS.Interfaces.Repositories;
using EMS.Models.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Respositories
{
    public class PaymentRepository : BaseRepository, IPaymentRepository
    {
        public PaymentRepository(EmsContext emsContext) : base(emsContext)
        {
        }

        public async Task<Payment?> GetByReferenceAsync(string reference)
        {
            return await _emsContext.Set<Payment>()
                .Include(p => p.Order)
                .ThenInclude(o => o.OrderItem)
                .ThenInclude(oi => oi.Item)
                .FirstOrDefaultAsync(p => p.Reference == reference);
        }

        public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
        {
            return await _emsContext.Set<Payment>()
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }
    }
}

