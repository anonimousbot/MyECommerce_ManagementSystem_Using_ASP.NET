using System.Security.Claims;
using EMS.Interfaces.Services;
using EMS.Models.Security;
using EMS.Models.DTOs.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
    public class CartController(ICartService cartService) : Controller
    {
        private readonly ICartService _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "User");
            }

            var cart = await _cartService.GetCartAsync(userId, CancellationToken.None);
            return View(cart);
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Json(new { count = 0 });
            }

            var cart = await _cartService.GetCartAsync(userId, CancellationToken.None);
            if (!cart.Status || cart.Data == null)
            {
                return Json(new { count = 0 });
            }

            var count = cart.Data.Items.Sum(item => item.Quantity);
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(Guid itemId, int quantity = 1, string? returnUrl = null)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "User");
            }

            var result = await _cartService.AddItemAsync(userId, itemId, quantity, CancellationToken.None);
            TempData[result.Status ? "Success" : "Error"] = result.Message;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(Guid itemId, int quantity)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "User");
            }

            var result = await _cartService.UpdateItemQuantityAsync(userId, itemId, quantity, CancellationToken.None);
            TempData[result.Status ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid itemId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "User");
            }

            var result = await _cartService.RemoveItemAsync(userId, itemId, CancellationToken.None);
            TempData[result.Status ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "User");
            }

            var cart = await _cartService.GetCartAsync(userId, CancellationToken.None);
            if (!cart.Status || cart.Data == null || cart.Data.Items.Count == 0)
            {
                TempData["Error"] = cart.Message ?? "Cart is empty.";
                return RedirectToAction("Index");
            }

            return View(new CheckoutCartViewModel
            {
                Cart = cart.Data,
                DeliveryAddress = string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutCartViewModel model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "User");
            }

            var cart = await _cartService.GetCartAsync(userId, CancellationToken.None);
            if (!cart.Status || cart.Data == null || cart.Data.Items.Count == 0)
            {
                TempData["Error"] = cart.Message ?? "Cart is empty.";
                return RedirectToAction("Index");
            }

            model.Cart = cart.Data;

            var callbackUrl = Url.Action("PaystackCallback", "Payment", null, Request.Scheme) ?? string.Empty;
            var init = await _cartService.CheckoutAsync(userId, email ?? string.Empty, model.DeliveryAddress, callbackUrl, CancellationToken.None);
            if (!init.Status || init.Data == null)
            {
                ModelState.AddModelError(string.Empty, init.Message ?? "Checkout failed.");
                return View(model);
            }

            return Redirect(init.Data.AuthorizationUrl);
        }
    }
}
