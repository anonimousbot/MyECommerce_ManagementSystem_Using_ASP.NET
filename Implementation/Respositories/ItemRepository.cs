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
    }
}
