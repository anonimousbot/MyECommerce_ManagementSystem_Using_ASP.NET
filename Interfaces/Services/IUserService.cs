using EMS.Models.DTOs;
using EMS.Models.DTOs.Users;

namespace EMS.Interfaces.Services
{
    public interface IUserService
    {
        public Task<BaseResponse<LoginResponseModel>> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken);
        public Task<BaseResponse<UserDto>> GetUserByEmail(string email, CancellationToken cancellationToken);
        //public Task<BaseResponse<bool>> DeleteAsync(Guid id);
        Task<BaseResponse<LoginResponseModel>> GoogleLoginOrRegisterAsync(GoogleUserDTO googleUser, CancellationToken cancellationToken);
        public Task<BaseResponse<UserDto>> GetUserProfileByUserId(Guid userId, CancellationToken cancellationToken);
        //public Task<BaseResponse<int>> GetAllHospitalStaffs(CancellationToken cancellationToken);
    }
}
