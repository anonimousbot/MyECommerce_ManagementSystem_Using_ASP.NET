using System.Linq.Expressions;
using EMS.Models.Entities;

namespace EMS.Interfaces.Repositories
{
    public interface IItemRepository : IBaseRepository
    {
        Task<bool> Any(Expression<Func<Item, bool>> expression);
        Task<Item> GetItemsByIdAsync(Guid itemId);
    }
}
