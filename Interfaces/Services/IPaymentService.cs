using EMS.Models.DTOs;
using EMS.Models.DTOs.Payments;

namespace EMS.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<BaseResponse<PaystackInitializeResultDto>> InitializePaystackAsync(Guid customerId, string email, decimal amount, string callbackUrl, string checkoutSnapshotJson, CancellationToken cancellationToken);
        Task<BaseResponse<PaymentVerificationResultDto>> VerifyPaystackAsync(string reference, CancellationToken cancellationToken);
        Task<BaseResponse<PaymentVerificationResultDto>> VerifyPaystackForUserAsync(Guid userId, string reference, CancellationToken cancellationToken);
    }
}
