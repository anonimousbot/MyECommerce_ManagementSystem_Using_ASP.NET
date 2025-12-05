using System.Linq.Expressions;
using EMS.Models.Entities;

namespace EMS.Interfaces.Repositories
{
    public interface IUserRepository : IBaseRepository
    {
        public Task<IReadOnlyList<User>> GetByRole(Expression<Func<User, bool>> expression);
        public Task<User> GetUserAndRoles(Guid userId);
        Task<User> GetUserByEmail(string email);
        Task<User> GetUserProfile(Guid userId);
        Task<bool> Any(Expression<Func<User, bool>> expression);
        //Task<User> GetUserByCustomerId(Guid customerId);
    }
}
