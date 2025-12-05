using System.Linq.Expressions;
using EMS.Interfaces.Repositories;
using EMS.Models.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Respositories
{
    public class RoleRepository : BaseRepository,IRoleRepository
    {
        public RoleRepository(EmsContext emsContext) : base(emsContext)
        {

        }
        public async Task<IEnumerable<Role>> GetRoles()
        {
            return await _emsContext.Set<Role>()
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Role>> GetRolesByIdsAsync(Expression<Func<Role, bool>> expression)
        {
           return await _emsContext.Set<Role>()
                .Where(expression)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
