using EMS.Implementation.Respositories;
using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Infrastructure.Storage;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Items;
using EMS.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace EMS.Implementation.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<ItemService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ItemService(
            IItemRepository itemRepository,
            ILogger<ItemService> logger,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _itemRepository= itemRepository;
            _logger= logger;
            _unitOfWork= unitOfWork;
            _environment = environment;
            _configuration = configuration;
            
        }

        private static bool IsSafeImageExtension(string? ext)
        {
            if (string.IsNullOrWhiteSpace(ext)) return false;
            ext = ext.ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";
        }

        private async Task<(bool ok, string? imagePath, string? error)> TrySaveItemImageAsync(
            IFormFile? image,
            CancellationToken cancellationToken)
        {
            if (image == null || image.Length <= 0)
            {
                return (true, null, null);
            }

            const long maxBytes = 5 * 1024 * 1024;
            if (image.Length > maxBytes)
            {
                return (false, null, "Image must be 5MB or smaller");
            }

            var ext = Path.GetExtension(image.FileName);
            if (!IsSafeImageExtension(ext))
            {
                return (false, null, "Image must be JPG, PNG, WEBP, or GIF");
            }

            if (!string.IsNullOrWhiteSpace(image.ContentType) &&
                !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "Invalid image content type");
            }

            var uploadDir = UploadStoragePaths.ResolveItemUploadsRoot(_configuration, _environment);
            Directory.CreateDirectory(uploadDir);

            var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var physicalPath = Path.Combine(uploadDir, fileName);

            await using (var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await image.CopyToAsync(stream, cancellationToken);
            }

            var requestPath = $"/uploads/items/{fileName}";
            return (true, requestPath, null);
        }

        private void TryDeleteItemImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;

            var fileName = Path.GetFileName(imagePath);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var physicalPath = Path.Combine(UploadStoragePaths.ResolveItemUploadsRoot(_configuration, _environment), fileName);
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
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

            var (ok, imagePath, error) = await TrySaveItemImageAsync(model.Image, CancellationToken.None);
            if (!ok)
            {
                _logger.LogWarning("Item image upload rejected: {Error}", error);
                return new BaseResponse<bool>
                {
                    Message = error ?? "Invalid image",
                    Status = false
                };
            }
            var item = new Item
            {
                Name = model.Name,
                Price = model.Price,
                Brand = model.Brand,
                QuantityInStock = model.QuantityInStock,
                ImagePath = imagePath,
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
                    Message ="Item coudn't be found",
                    Status = false
                };
            }

            TryDeleteItemImage(getItem.ImagePath);
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
                    ImagePath = getItem.ImagePath,

                }
            };
        }
        public async Task<BaseResponse<IEnumerable<ItemDto>>> GetItemAsync(CancellationToken token, int pageNumber = 1, int pageSize = 10, string? searchString = null)
        {
            var (normalizedPageNumber, normalizedPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
            var query = _itemRepository.Query<Item>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var normalizedSearch = searchString.Trim();
                query = query.Where(i =>
                    (!string.IsNullOrWhiteSpace(i.Name) && EF.Functions.Like(i.Name, $"%{normalizedSearch}%")) ||
                    (!string.IsNullOrWhiteSpace(i.Brand) && EF.Functions.Like(i.Brand, $"%{normalizedSearch}%")));
            }

            var totalItems = await query.CountAsync(token);
            var pagedItems = await query
                .OrderByDescending(i => i.DateCreated)
                .Select(i => new ItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    DateCreated = i.DateCreated,
                    Brand = i.Brand,
                    Price = i.Price,
                    QuantityInStock = i.QuantityInStock,
                    ImagePath = i.ImagePath,
                })
                .Skip((normalizedPageNumber - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToListAsync(token);

            if (totalItems == 0)
            {
                _logger.LogError("Items couldn't be found");
                return new BaseResponse<IEnumerable<ItemDto>>
                {
                    Message = "Items couldn't be found",
                    Status = false,
                    Data = [],
                    Pagination = PaginationHelper.Create(normalizedPageNumber, normalizedPageSize, totalItems)
                };
            }

            return new BaseResponse<IEnumerable<ItemDto>>
            {
                Message = "Items Gotten Successfully",
                Status = true,
                Data = pagedItems,
                Pagination = PaginationHelper.Create(normalizedPageNumber, normalizedPageSize, totalItems)
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

            string? newImagePath = null;
            if (model.Image != null)
            {
                var (ok, imagePath, error) = await TrySaveItemImageAsync(model.Image, CancellationToken.None);
                if (!ok)
                {
                    _logger.LogWarning("Item image upload rejected: {Error}", error);
                    return new BaseResponse<ItemDto>
                    {
                        Message = error ?? "Invalid image",
                        Status = false
                    };
                }
                newImagePath = imagePath;
            }
            
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                getItem.Name = model.Name;
            }

            if (!string.IsNullOrWhiteSpace(model.Brand))
            {
                getItem.Brand = model.Brand;
            }

            if (model.Price > 0)
            {
                getItem.Price = model.Price;
            }

            if (model.QuantityInStock >= 0)
            {
                getItem.QuantityInStock = model.QuantityInStock;
            }
            if (!string.IsNullOrWhiteSpace(newImagePath))
            {
                TryDeleteItemImage(getItem.ImagePath);
                getItem.ImagePath = newImagePath;
            }
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
                    ImagePath = getItem.ImagePath,
                    DateModified = getItem.DateModified
                }
            };
        }
    }
}
