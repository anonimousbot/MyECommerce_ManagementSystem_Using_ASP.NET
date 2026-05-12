using EMS.Models.Entities;

namespace EMS.Interfaces.Repositories
{
    public interface IPaymentRepository : IBaseRepository
    {
        Task<Payment?> GetByReferenceAsync(string reference);
        Task<Payment?> GetByOrderIdAsync(Guid orderId);
    }
}

