using System.Linq.Expressions;
using System.Threading.Tasks;
using EMS.Contracts.Entities;
using EMS.Interfaces.Repositories;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;


namespace EMS.Implementation.Respositories
{
    public class BaseRepository : IBaseRepository
    {
        protected readonly EmsContext _emsContext;

        public BaseRepository(EmsContext emsContext) 
        {
            _emsContext = emsContext ?? throw new ArgumentNullException(nameof(emsContext));
        }
        public virtual async Task<T> Add<T>(T entity) where T : BaseEntity
        {
            var entry = await _emsContext.Set<T>().AddAsync(entity);
            return entry.Entity;
        }

        public virtual void Delete<T>(T entity) where T : BaseEntity
        {
            if (entity == null) return;

            var entry = _emsContext.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _emsContext.Set<T>().Attach(entity);
            }
            
            _emsContext.Set<T>().Remove(entity);
        }

        public virtual async Task<T> Get<T>(Expression<Func<T, bool>> expression) where T : BaseEntity
        {
            return await _emsContext.Set<T>().FirstOrDefaultAsync(expression);
        }

        public virtual async Task<IReadOnlyList<T>> GetAll<T>() where T : BaseEntity
        {
            return await _emsContext.Set<T>()
                   .ToListAsync();

        }

        public virtual IQueryable<T> QueryWhere<T>(Expression<Func<T, bool>> expression) where T : BaseEntity
        {
            return _emsContext.Set<T>().Where(expression);
        }

        public virtual void Update<T>(T entity) where T : BaseEntity
        {
            if (entity == null) return;

            // Ensure entity is attached and marked Modified so EF will persist property changes.
            var entry = _emsContext.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _emsContext.Set<T>().Attach(entity);
                entry.State = EntityState.Modified;
            }
            else
            {
                entry.State = EntityState.Modified;
            }
        }
    }
}
