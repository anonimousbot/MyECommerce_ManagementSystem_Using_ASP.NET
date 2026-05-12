using EMS.Interfaces.Services;
using EMS.Models.DTOs.Items;
using EMS.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Controllers
{
    public class ItemController(IItemService itemService, ILogger<ItemController> logger) : Controller
    {
        private readonly ILogger<ItemController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IItemService _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));

        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            var items = await _itemService.GetItemAsync(CancellationToken.None, pageNumber, pageSize);
            return View(items);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateItemRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var item = await _itemService.CreateAsync(model);
            if (!item.Status)
            {
                ModelState.AddModelError(string.Empty, item.Message ?? "Item creation unsuccessful");
                return View(model);
            }

            TempData["Success"] = "Item created successfully";
            return RedirectToAction("Index", "Item");
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var item = await _itemService.GetByIdAsync(id, CancellationToken.None);
            if (item == null)
            {
                _logger.LogError("Id Not Found {Id}", id);
                return NotFound();
            }
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] Guid id)
        {
            var item = await _itemService.DeleteAsync(id, CancellationToken.None);
            if (item == null)
            {
                return NotFound();
            }
            return RedirectToAction("Index", "Item");
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetItemById(Guid id)
        {
            var item = await _itemService.GetByIdAsync(id, CancellationToken.None);
            if (item == null || item.Data == null)
            {
                return NotFound();
            }
            return View(item);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Update(Guid id)
        {
            var item = await _itemService.GetByIdAsync(id, CancellationToken.None);
            if (item == null || item.Data == null)
            {
                return NotFound();
            }

            return View(new UpdateItemViewModel
            {
                Id = item.Data.Id,
                Name = item.Data.Name,
                Brand = item.Data.Brand,
                Price = item.Data.Price,
                QuantityInStock = item.Data.QuantityInStock,
                CurrentImagePath = item.Data.ImagePath
            });
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UpdateItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var updateRequest = new UpdateItemRequestModel
            {
                Name = model.Name,
                Brand = model.Brand,
                Price = model.Price,
                QuantityInStock = model.QuantityInStock,
                Image = model.Image
            };

            var updateItem = await _itemService.UpdateAsync(model.Id, updateRequest);
            if (updateItem == null || !updateItem.Status)
            {
                ModelState.AddModelError(string.Empty, updateItem?.Message ?? "Update failed");
                if (string.IsNullOrWhiteSpace(model.CurrentImagePath))
                {
                    var itemResp = await _itemService.GetByIdAsync(model.Id, CancellationToken.None);
                    model.CurrentImagePath = itemResp?.Data?.ImagePath;
                }

                return View(model);
            }

            TempData["Success"] = "Item updated successfully";
            return RedirectToAction("Index", "Item");
        }
    }
}
