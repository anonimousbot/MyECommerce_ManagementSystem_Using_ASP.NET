using EMS.Models.DTOs;
using EMS.Models.DTOs.Customers;
using EMS.Models.DTOs.Orders;

namespace EMS.Interfaces.Services
{
    public interface IOrderService 
    {
        Task<BaseResponse<bool>> CreateAsync(CreateOrderRequestModel model);
        Task<BaseResponse<bool>> UpdateAsync(Guid id,UpdateOrderRequestModel model);
        Task<BaseResponse<IEnumerable<OrderDto>>> GetOrderAsync(CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 10);
        Task<BaseResponse<IEnumerable<OrderDto>>> GetOrdersByCustomerAsync(Guid id,  CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 10);
        Task<BaseResponse<OrderDto>> GetOrderById(Guid id,CancellationToken cancellationtoken);
        Task<BaseResponse<IReadOnlyList<OrderDto>>> GetPendingOrderAsync(CancellationToken cancellationtoken);
        Task<BaseResponse<IReadOnlyList<OrderDto>>> GetProcessingOrderAsync(CancellationToken cancellationtoken);
        Task<BaseResponse<IReadOnlyList<OrderDto>>> GetDeliveredOrderAsync(CancellationToken cancellationtoken);
        Task<BaseResponse<IReadOnlyList<OrderDto>>> GetCancelledOrderAsync(CancellationToken cancellationtoken);
        Task<BaseResponse<bool>> DeleteAsync(Guid orderId);
    }
}
