using EMS.Models.DTOs;
using EMS.Models.DTOs.Customers;
using EMS.Models.DTOs.Items;
using EMS.Models.DTOs.Users;
using EMS.Models.Entities;

namespace EMS.Interfaces.Services
{
    public interface ICustomerService
    {
        Task<BaseResponse<bool>> CreateAsync(CreateCustomerRequestModel request);
        Task<BaseResponse<bool>> CreateCustomerByGmail(Customer createCustomer);
        Task<BaseResponse<CustomerDto>> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);
        Task<IReadOnlyList<CustomerDto>> GetAsync(string param, CancellationToken cancellationToken);
        Task<BaseResponse<IReadOnlyList<CustomerDto>>> GetCustomerAsync(CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 10);
        Task<BaseResponse<CustomerDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task<BaseResponse<bool>> UpdateAsync(UpdateCustomerRequestModel model, Guid id);
        Task<BaseResponse<bool>> DeleteAsync(Guid id);
        Task<BaseResponse<LoginResponseModel>> GoogleLoginOrRegisterAsync(GoogleUserDTO googleUser,CancellationToken cancellationToken);
    }
}
