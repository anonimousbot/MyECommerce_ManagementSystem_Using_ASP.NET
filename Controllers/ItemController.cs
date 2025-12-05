using EMS.Implementation.Services;
using EMS.Interfaces.Services;
using EMS.Models.DTOs.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Controllers
{
    public class ItemController(IItemService itemService, ILogger<ItemService> logger) : Controller
    {
        private readonly ILogger<ItemService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IItemService _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var items = await _itemService.GetItemAsync(CancellationToken.None);
            return View(items);
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateItemRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);   
            }
            var item = await _itemService.CreateAsync(model);
            if (!item.Status)
            {
                ViewBag.Failed = "item creation unsuccessful";
            }
            ViewBag.Success = "item created successfully";

            return RedirectToAction("Index","Item");
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public  async Task<IActionResult> Delete([FromRoute]Guid id)
        {
            
            var item = await _itemService.GetByIdAsync(id, CancellationToken.None);
            if (item == null)
            {
                _logger.LogError($"Id Not Found {id}");
                return NotFound();
            }
            return View(item);
        }
        [HttpPost,ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed([FromRoute]Guid id)
        {
            var item = await _itemService.DeleteAsync(id,CancellationToken.None);
            if (item == null)
            {
                return NotFound();
            }
            return RedirectToAction("Index", "Item");

        }
        [HttpGet]
        public async Task<IActionResult> GetItemById(Guid id)
        {
            var item = await _itemService.GetByIdAsync(id, CancellationToken.None);
            if(item == null || item.Data == null)
            {
                return NotFound();
            }
            return View(item);
        }
        [HttpGet]
        public async Task<IActionResult> Update(Guid id)
        {
            var item = await _itemService.GetByIdAsync(id, CancellationToken.None);
            if (item == null || item.Data == null)
            {
                return NotFound();
            }
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Update(Guid id, UpdateItemRequestModel model)
        {
            // Make sure the incoming form bound model is valid
            if (!ModelState.IsValid)
            {
                var itemResp = await _itemService.GetByIdAsync(id, CancellationToken.None);
                return View(itemResp);
            }

            var updateItem = await _itemService.UpdateAsync(id, model);
            if (updateItem == null || !updateItem.Status)
            {
                // show the error and return the form with the current entity data
                ModelState.AddModelError(string.Empty, updateItem?.Message ?? "Update failed");
                var itemResp = await _itemService.GetByIdAsync(id, CancellationToken.None);
                return View(itemResp);
            }

            return RedirectToAction("Index","Item");
        }

    }
}
