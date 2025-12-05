using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Roles;

namespace EMS.Implementation.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IRoleRepository roleRepository, ILogger<RoleService> logger)
        {
            _logger = logger;
            _roleRepository = roleRepository;
        }
        public Task<bool> DeleteAsync(Guid roleId)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<IReadOnlyList<RoleDto>>> GetAsync(string param, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<RoleDto>> GetByIdAsync(Guid roleId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<IEnumerable<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken)
        {
            var roles = await _roleRepository.GetRoles();
            if (!roles.Any())
            {
                _logger.LogError("No roles found");
                return new BaseResponse<IEnumerable<RoleDto>>
                {
                    Message = "No roles found",
                    Status = false
                };
            }

            _logger.LogInformation("Roles fetched successfully");
            return new BaseResponse<IEnumerable<RoleDto>>
            {
                Message = "Roles fetched successfully",
                Status = true,
                Data = roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DateCreated = r.DateCreated,
                })
            };
        }
    }
}
