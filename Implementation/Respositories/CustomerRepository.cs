using System.Numerics;
using EMS.Interfaces.Repositories;
using EMS.Models.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Respositories
{
    public class CustomerRepository : BaseRepository, ICustomerRepository
    {
        public CustomerRepository(EmsContext emsContext) : base(emsContext)
        {

        }

        public async Task<Customer> CheckCustomerWithUser(Guid customerId)
        {
           return await _emsContext.Set<Customer>()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == customerId);
        }

        public async Task<Customer> GetCustomersByIdAsync(Guid customerId)
        {
            return await _emsContext.Set<Customer>()
               .Include(d => d.User)
               .Include(c => c.Orders)
               .AsNoTracking()
               .FirstOrDefaultAsync(d => d.Id == customerId);
        }
    }
}
