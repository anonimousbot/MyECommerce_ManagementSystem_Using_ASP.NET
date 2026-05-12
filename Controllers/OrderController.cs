using System.Security.Claims;
using EMS.Interfaces.Services;
using EMS.Models.DTOs.Orders;
using EMS.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IItemService _itemService;

        public OrderController(IOrderService orderService, IItemService itemService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _itemService  = itemService  ?? throw new ArgumentNullException(nameof(itemService));
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            var getOrders = await _orderService.GetOrderAsync(CancellationToken.None, pageNumber, pageSize);
            if (getOrders == null) return NotFound();
            return View(getOrders);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        public async Task<IActionResult> CreateOrder(Guid itemId)
        {
            var items = await _itemService.GetByIdAsync(itemId, CancellationToken.None);
            if (items?.Data == null) return NotFound();

            var model = new CreateOrderRequestModel
            {
                ItemId   = items.Data.Id,
                Amount   = items.Data.Price,
                Quantity = 1,
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        public async Task<IActionResult> CreateOrder(CreateOrderRequestModel model)
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claimValue) || !Guid.TryParse(claimValue, out var userGuid))
            {
                ModelState.AddModelError(string.Empty, "Unable to determine customer identity. Please sign in again.");
                return View(model);
            }
            model.CustomerId = userGuid;

            if (!TryValidateModel(model)) return View(model);

            // Re-fetch server-side price — never trust client-submitted amounts
            var item = await _itemService.GetByIdAsync(model.ItemId, CancellationToken.None);
            if (item?.Data == null)
            {
                ModelState.AddModelError(string.Empty, "Item not found.");
                return View(model);
            }
            model.Amount = item.Data.Price;

            var result = await _orderService.CreateAsync(model);
            if (!result.Status)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Order creation unsuccessful.");
                return View(model);
            }

            TempData["Success"] = result.Message ?? "Order placed successfully.";
            return RedirectToAction("Index", "Customer");
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var order = await _orderService.GetOrderById(id, CancellationToken.None);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder([FromRoute] Guid id)
        {
            var deleteOrder = await _orderService.DeleteAsync(id);
            TempData[deleteOrder.Status ? "Success" : "Error"] =
                deleteOrder.Status ? "Order deleted successfully." : "Order couldn't be deleted.";
            return RedirectToAction("Index", "Order");
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        public async Task<IActionResult> GetOrdersByCustomerId(int pageNumber = 1, int pageSize = 10)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var customerId))
                return RedirectToAction("Login", "User");

            var getOrder = await _orderService.GetOrdersByCustomerAsync(customerId, CancellationToken.None, pageNumber, pageSize);
            if (!getOrder.Status)
                TempData["Error"] = "We couldn't find your orders.";

            return View(getOrder);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateOrder(Guid id)
        {
            var order = await _orderService.GetOrderById(id, CancellationToken.None);
            if (order == null) return NotFound();
            ViewBag.OrderStatus = order.Data?.OrderStatus;
            return View(order);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(Guid id, UpdateOrderRequestModel model)
        {
            var updateOrder = await _orderService.UpdateAsync(id, model);
            if (updateOrder == null) return BadRequest();
            return RedirectToAction("Index", "Item");
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetAllCancelledOrder()
        {
            var getOrder = await _orderService.GetCancelledOrderAsync(CancellationToken.None);
            if (getOrder == null) return NotFound();
            return View(getOrder);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetAllProcessingOrder()
        {
            var getOrder = await _orderService.GetProcessingOrderAsync(CancellationToken.None);
            if (getOrder == null) return NotFound();
            return View(getOrder);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetAllPendingOrder()
        {
            var getOrder = await _orderService.GetPendingOrderAsync(CancellationToken.None);
            if (getOrder == null) return NotFound();
            return View(getOrder);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetAllDeliveredOrder()
        {
            var getOrder = await _orderService.GetDeliveredOrderAsync(CancellationToken.None);
            if (getOrder == null) return NotFound();
            return View(getOrder);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetOrdersById(Guid id)
        {
            var order = await _orderService.GetOrderById(id, CancellationToken.None);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
