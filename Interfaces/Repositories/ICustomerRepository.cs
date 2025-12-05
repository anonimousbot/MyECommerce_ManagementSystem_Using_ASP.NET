using System.Numerics;
using EMS.Models.Entities;
using EMS.Persistence.Context;

namespace EMS.Interfaces.Repositories
{
    public interface ICustomerRepository : IBaseRepository
    {
        public Task<Customer> GetCustomersByIdAsync(Guid customerId);
        public Task<Customer> CheckCustomerWithUser(Guid customerId);

    }
}
