using System.Linq.Expressions;
using EMS.Interfaces.Repositories;
using EMS.Models.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Respositories
{
    public class UserRepository : BaseRepository, IUserRepository
    {
        public UserRepository(EmsContext emsContext) : base(emsContext)
        {
        }
        public async Task<bool> Any(Expression<Func<User, bool>> expression)
        {
            return await _emsContext.Set<User>()
                .AnyAsync(expression);
        }

        public async Task<IReadOnlyList<User>> GetByRole(Expression<Func<User, bool>> expression)
        {
            return await _emsContext.Set<User>()
                .Where(expression)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<User> GetUserAndRoles(Guid userId)
        {
            return await _emsContext.Set<User>()
                 .Where(i => i.Id == userId)
                 .Include(u => u.UserRoles)
                 .ThenInclude(r => r.Role)
                 .SingleOrDefaultAsync();
        }
        public async Task<User> GetUserProfile(Guid userId)
        {
            return await _emsContext.Set<User>()
                .Where(u => u.Id == userId)
                .Include(a => a.Admin)
                .Include(p => p.Customer)
                .ThenInclude(p => p.Orders)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                 .AsSplitQuery()
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }
        public async Task<User> GetUserByEmail(string email)
        {
            return await _emsContext.Set<User>()
                .Include(u => u.UserRoles)
                .ThenInclude(r => r.Role)
                .Include(a => a.Admin)
                .Include(c => c.Customer)
                .SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetUserByGoogleId(string googleId)
        {
            return await _emsContext.Set<User>()
                .Include(u => u.UserRoles)
                .ThenInclude(r => r.Role)
                .Include(a => a.Admin)
                .Include(c => c.Customer)
                .SingleOrDefaultAsync(u => u.GoogleId == googleId);
        }
    }
}
