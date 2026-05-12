using EMS.Models.DTOs;
using EMS.Models.DTOs.Items;

namespace EMS.Interfaces.Services
{
    public interface IItemService
    {
        Task<BaseResponse<bool>> CreateAsync(CreateItemRequestModel model);
        Task<BaseResponse<ItemDto>> GetByIdAsync(Guid itemid, CancellationToken token);
        Task<BaseResponse<ItemDto>> UpdateAsync(Guid id, UpdateItemRequestModel model);
        Task<BaseResponse<IEnumerable<ItemDto>>> GetItemAsync(CancellationToken token, int pageNumber = 1, int pageSize = 10, string? searchString = null);
        Task<BaseResponse<bool>> DeleteAsync (Guid itemid, CancellationToken token);
    }
}
