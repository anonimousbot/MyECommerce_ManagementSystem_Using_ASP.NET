using EMS.Implementation.Respositories;
using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Items;
using EMS.Models.Entities;

namespace EMS.Implementation.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<ItemService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public ItemService(IItemRepository itemRepository,ILogger<ItemService> logger,IUnitOfWork unitOfWork)
        {
            _itemRepository= itemRepository;
            _logger= logger;
            _unitOfWork= unitOfWork;
            
        }

        public async Task<BaseResponse<bool>> CreateAsync(CreateItemRequestModel model)
        {
            var itemExist = await _itemRepository.Any(x => x.Name == model.Name);
            if (itemExist)
            {
                _logger.LogError("Item already exists");
                return new BaseResponse<bool>
                {
                    Message = "Item already exists",
                    Status = false
                };
            }
            var item = new Item
            {
                Name = model.Name,
                Price = model.Price,
                Brand = model.Brand,
                QuantityInStock = model.QuantityInStock,
                DateCreated = DateTime.UtcNow
            };
            var createItem = await _itemRepository.Add<Item>(item);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            if (createItem == null)
            {
                _logger.LogError("Item couldn't be created");
                return new BaseResponse<bool>
                {
                    Message = "Item couldn't be created",
                    Status = false
                };
            }
            return new BaseResponse<bool>
            {
                Message = "Item Created Successfully",
                Status = true
            };

        }

        public async Task<BaseResponse<bool>> DeleteAsync(Guid itemid, CancellationToken token)
        {
            var getItem = await _itemRepository.Get<Item>(i => i.Id == itemid);
            if (getItem == null)
            {
                _logger.LogError("Item coudn't be found");
                return new BaseResponse<bool>
                {
                    Message = "Item coudn't be found",
                    Status = false
                };
            }
               _itemRepository.Delete<Item>(getItem);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            _logger.LogInformation("Item Deleted Successfully");
            return new BaseResponse<bool>
            {
                Message = "Item Deleted Successfully",
                Status = true
            };
        }

        public async Task<BaseResponse<ItemDto>> GetByIdAsync(Guid itemId, CancellationToken token)
        {
            var getItem = await _itemRepository.GetItemsByIdAsync(itemId);
            if (getItem == null)
            {
                _logger.LogError("Item coudn't be found");
                return new BaseResponse<ItemDto>
                {
                    Message = "Item coudn't be found",
                    Status = false
                };
            }
            _logger.LogInformation($"Item fetched Successfully");
            return new BaseResponse<ItemDto>
            {
                Message = "Data Fetched",
                Status = true,
                Data = new ItemDto
                {
                    Id = getItem.Id,
                    Name = getItem.Name,
                    DateCreated = getItem.DateCreated,
                    Brand = getItem.Brand,
                    Price = getItem.Price,
                    QuantityInStock = getItem.QuantityInStock,

                }
            };
        }

        public async Task<BaseResponse<IEnumerable<ItemDto>>> GetItemAsync(CancellationToken token)
        {
            var getItems = await _itemRepository.GetAll<Item>();
            if (getItems == null)
            {
                _logger.LogError("Items couldn't be found");
                return new BaseResponse<IEnumerable<ItemDto>>
                {
                    Message = "Items couldn't be found",
                    Status = false
                };
            }
            return new BaseResponse<IEnumerable<ItemDto>>
            {
                Message = "Items Gotten Successfully",
                Status = true,
                Data = getItems.Select(i => new ItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    DateCreated = i.DateCreated,
                    Brand = i.Brand,
                    Price = i.Price,
                    QuantityInStock = i.QuantityInStock,
                    
                }).ToList()
            };
        }

        public async Task<BaseResponse<ItemDto>> UpdateAsync(Guid id, UpdateItemRequestModel model)
        {
            var getItem = await _itemRepository.Get<Item>(i =>  i.Id == id);
            if (getItem == null)
            {
                _logger.LogError("Item Couldn't be found");
                return new BaseResponse<ItemDto>
                {
                    Message = "Item Couldn't be found",
                    Status = false
                };
            }
            if (model == null)
            {
                return new BaseResponse<ItemDto>
                {
                    Message = "Fields cannot be empty",
                    Status = false,
                };
            }
            
            getItem.Name = model.Name ?? getItem.Name;
            getItem.Brand = model.Brand ?? getItem.Brand;
            getItem.Price = model.Price;
            getItem.QuantityInStock = model.QuantityInStock ;
            getItem.DateModified = DateTime.UtcNow;

            _itemRepository.Update(getItem);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            return new BaseResponse<ItemDto>
            {
                Message = "Item Updated Successfully",
                Status = true,
                Data = new ItemDto
                {
                    Id = getItem.Id,
                    Name = getItem.Name,
                    Brand = getItem.Brand,
                    Price = getItem.Price,
                    QuantityInStock = getItem.QuantityInStock,
                    DateModified = getItem.DateModified
                }
            };
        }
    }
}