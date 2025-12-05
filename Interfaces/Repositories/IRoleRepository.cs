using System.Linq.Expressions;
using EMS.Models.Entities;

namespace EMS.Interfaces.Repositories
{
    public interface IRoleRepository 
    {
        Task<IEnumerable<Role>> GetRolesByIdsAsync(Expression<Func<Role, bool>> expression);
        Task<IEnumerable<Role>> GetRoles();
    }
}
