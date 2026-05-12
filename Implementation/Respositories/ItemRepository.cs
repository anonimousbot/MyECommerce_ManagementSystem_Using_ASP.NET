using System.Linq.Expressions;
using EMS.Interfaces.Repositories;
using EMS.Models.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Respositories
{
    public class ItemRepository : BaseRepository, IItemRepository
    {
        public ItemRepository(EmsContext emsContext) : base(emsContext)
        {

        }

        public async Task<bool> Any(Expression<Func<Item, bool>> expression)
        {
            return await _emsContext.Set<Item>()
                .AnyAsync(expression);

        }
        public async Task<Item> GetItemsByIdAsync(Guid itemId)
        {
            return await _emsContext.Set<Item>()
               .AsNoTracking()
               .FirstOrDefaultAsync(d => d.Id == itemId);
        }

        public async Task<bool> TryDecrementStockAsync(Guid itemId, int quantity, CancellationToken cancellationToken)
        {
            if (quantity <= 0) return true;

            var affected = await _emsContext.Set<Item>()
                .Where(i => i.Id == itemId && i.QuantityInStock >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.QuantityInStock, i => i.QuantityInStock - quantity)
                    .SetProperty(i => i.DateModified, _ => DateTime.UtcNow),
                    cancellationToken);

            return affected == 1;
        }

        public async Task IncrementStockAsync(Guid itemId, int quantity, CancellationToken cancellationToken)
        {
            if (quantity <= 0) return;

            await _emsContext.Set<Item>()
                .Where(i => i.Id == itemId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.QuantityInStock, i => i.QuantityInStock + quantity)
                    .SetProperty(i => i.DateModified, _ => DateTime.UtcNow),
                    cancellationToken);
        }
    }
}
