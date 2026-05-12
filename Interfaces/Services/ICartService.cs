using EMS.Models.DTOs;
using EMS.Models.DTOs.Carts;
using EMS.Models.DTOs.Payments;

namespace EMS.Interfaces.Services
{
    public interface ICartService
    {
        Task<BaseResponse<CartDto>> GetCartAsync(Guid userId, CancellationToken cancellationToken);
        Task<BaseResponse<bool>> AddItemAsync(Guid userId, Guid itemId, int quantity, CancellationToken cancellationToken);
        Task<BaseResponse<bool>> UpdateItemQuantityAsync(Guid userId, Guid itemId, int quantity, CancellationToken cancellationToken);
        Task<BaseResponse<bool>> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);
        Task<BaseResponse<PaystackInitializeResultDto>> CheckoutAsync(Guid userId, string email, string deliveryAddress, string callbackUrl, CancellationToken cancellationToken);
    }
}
