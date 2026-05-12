using Microsoft.EntityFrameworkCore.Storage;

namespace EMS.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginTransactionAsync();
        void ClearTracking();
    }
}
