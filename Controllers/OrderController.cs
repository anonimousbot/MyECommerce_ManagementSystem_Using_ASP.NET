using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EMS.Interfaces.Services;
using EMS.Models.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderSevice;
        private readonly IItemService _itemService;
        public OrderController(IOrderService orderService, IItemService itemService)
        {
            _orderSevice = orderService;
            _itemService = itemService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var getOrders = await _orderSevice.GetOrderAsync(CancellationToken.None);
            if (getOrders == null)
            {
                return NotFound();
            }
            return View(getOrders);
        }

        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder(Guid itemId)
        {
            var items = await _itemService.GetByIdAsync(itemId, CancellationToken.None);
            if (items?.Data == null)
                return NotFound();

            var model = new CreateOrderRequestModel
            {
                ItemId = items.Data.Id,
                Amount = items.Data.Price,
                Quantity = 1,
                // TotalAmount computed by DTO
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder(CreateOrderRequestModel model)
        {
            // Resolve authenticated user's GUID from claim and set on model
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claimValue) || !Guid.TryParse(claimValue, out var userGuid))
            {
                ModelState.AddModelError(string.Empty, "Unable to determine customer identity. Please sign in again.");
                return View(model);
            }
            model.CustomerId = userGuid;
            if (!TryValidateModel(model))
            {
                return View(model);
            }

            // Recompute server-side total if needed (optional)
            // model.TotalAmount is computed property; avoid trusting client Amount/Quantity
            var item = await _itemService.GetByIdAsync(model.ItemId, CancellationToken.None);
            if (item?.Data == null)
            {
                ModelState.AddModelError(string.Empty, "Item not found.");
                return View(model);
            }
            model.Amount = item.Data.Price;

            var result = await _orderSevice.CreateAsync(model);
            if (!result.Status)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Order creation unsuccessful");
                return View(model);
            }

            TempData["Success"] = result.Message ?? "Order placed";
            return RedirectToAction("Index","Customer");
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {

            var order = await _orderSevice.GetOrderById(id, CancellationToken.None);
            if (order == null)
            {

                return NotFound();
            }
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder([FromRoute] Guid id)
        {
            var deleteOrder = await _orderSevice.DeleteAsync(id);
            if (!deleteOrder.Status)
            {
                ViewBag.Failed = "Order Couldn't Be Deleted";
            }
            ViewBag.Success = "Order Deleted Successfully";
            return RedirectToAction("Index", "Order");
        }
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetOrdersByCustomerId()
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var getOrder = await _orderSevice.GetOrdersByCustomerAsync(Guid.Parse(customerId), CancellationToken.None);
            if (!getOrder.Status)
            {
                ViewBag.Failed = "We Cant find the Order";
            }
            ViewBag.Success = "We Can find the Order";
            return View(getOrder);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrder(Guid id)
        {
            var order = await _orderSevice.GetOrderById(id, CancellationToken.None);
            if (order == null)
            {
                return NotFound();
            }
            ViewBag.OrderStatus = order.Data.OrderStatus;
            return View(order);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrder(Guid id, UpdateOrderRequestModel model)
        {
            var updateOrder = await _orderSevice.UpdateAsync(id, model);
            if (updateOrder == null)
            {
                return BadRequest();
            }
            return RedirectToAction("Index", "Item");
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCancelledOrder()
        {
            var getOrder = await _orderSevice.GetCancelledOrderAsync(CancellationToken.None);
            if (getOrder == null)
            {
                return NotFound();
            }
            return View(getOrder);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllProcessingOrder()
        {
            var getOrder = await _orderSevice.GetProcessingOrderAsync(CancellationToken.None);
            if (getOrder == null)
            {
                return NotFound();
            }
            return View(getOrder);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPendingOrder()
        {
            var getOrder = await _orderSevice.GetPendingOrderAsync(CancellationToken.None);
            if (getOrder == null)
            {
                return NotFound();
            }
            return View(getOrder);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllDeliveredOrder()
        {
            var getOrder = await _orderSevice.GetDeliveredOrderAsync(CancellationToken.None);
            if (getOrder == null)
            {
                return NotFound();
            }
            return View(getOrder);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrdersById(Guid id)
        {
            var order = await _orderSevice.GetOrderById(id, CancellationToken.None);
            if(order == null)
            {
                return NotFound();   
            }
            return View(order);
        }
    }
}
