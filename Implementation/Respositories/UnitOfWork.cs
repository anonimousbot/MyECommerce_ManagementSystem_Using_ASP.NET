using EMS.Interfaces.Repositories;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace EMS.Implementation.Respositories
{
    public class UnitOfWork :  IUnitOfWork
    {
        private readonly EmsContext _context;
        public UnitOfWork(EmsContext context)
        {
            _context = context ?? throw new ArgumentNullException (nameof(context));
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return  await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
