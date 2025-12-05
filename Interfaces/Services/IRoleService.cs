using EMS.Models.DTOs;
using EMS.Models.DTOs.Roles;

namespace EMS.Interfaces.Services
{
    public interface IRoleService
    {
        Task<BaseResponse<RoleDto>> GetByIdAsync(Guid roleId, CancellationToken cancellationToken);
        Task<BaseResponse<IReadOnlyList<RoleDto>>> GetAsync(string param, CancellationToken cancellationToken);
        Task<BaseResponse<IEnumerable<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid roleId);
    }
}
