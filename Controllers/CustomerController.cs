using System.Security.Claims;
using EMS.Interfaces.Services;
using EMS.Models.DTOs.Customers;
using EMS.Models.Enums;
using EMS.Models.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Controllers
{
    public class CustomerController(ICustomerService customerService,IRoleService roleService,
        ILogger<CustomerController> logger, IUserService userService,IItemService itemService) : Controller
    {
        private readonly IItemService _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
        private readonly ICustomerService _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        private readonly IRoleService roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        private readonly ILogger<CustomerController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));

        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 9)
        {
            ViewBag.CurrentSearch = searchString;
            var items = await _itemService.GetItemAsync(CancellationToken.None, pageNumber, pageSize, searchString);
            return View(items);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]    
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCustomerRequestModel model)
        {
            if (!TryValidateModel(model))
            {
                return View(model);
            }
            var customer = await _customerService.CreateAsync(model);
            if (customer.Status)
            {
                ViewBag.Alert = customer.Status;
                ViewBag.AlertType = "success";
                return RedirectToAction("Login","User");
            }
            else
            {

                ViewBag.Alert = customer.Status;
                ViewBag.AlertType = "danger";
                ModelState.AddModelError("ConfirmPassword", customer.Message);
                return View(model);

            }
            

        }
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        public async Task<IActionResult> Update(Guid id)
        {
            var customerCheck = await EnsureCustomerOwnsResourceAsync(id);
            if (customerCheck is not null)
            {
                return customerCheck;
            }

            var getCustomer = await _customerService.GetByIdAsync(id, CancellationToken.None);
            if (getCustomer == null || !getCustomer.Status || getCustomer.Data == null)
            {
                return NotFound();
            }

            return View(new UpdateCustomerRequestModel
            {
                FirstName = getCustomer.Data.FirstName,
                LastName = getCustomer.Data.LastName,
                Address = getCustomer.Data.Address,
                PhoneNumber = getCustomer.Data.PhoneNumber,
                Gender = getCustomer.Data.Gender
            });
        }
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UpdateCustomerRequestModel model,Guid id)
        {
            var customerCheck = await EnsureCustomerOwnsResourceAsync(id);
            if (customerCheck is not null)
            {
                return customerCheck;
            }

            var updateCustomer = await _customerService.UpdateAsync(model, id);
            if (updateCustomer == null || !updateCustomer.Status)
            {
                ModelState.AddModelError(string.Empty, updateCustomer?.Message ?? "Unable to update profile.");
                return View(model);
            }
            return RedirectToAction("CustomerProfile","Customer");
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        public async Task<IActionResult> CustomerProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine(User.FindFirstValue(ClaimTypes.GivenName));

            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "User");
            }
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return BadRequest("Invalid user ID format.");
            }

            var customer = await _userService.GetUserProfileByUserId(userId, CancellationToken.None);

            if (customer == null || !customer.Status) return NotFound(customer.Message);

            return View(customer);
        }
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetAllCustomer(int pageNumber = 1, int pageSize = 10)
        {
            var getAll = await _customerService.GetCustomerAsync(CancellationToken.None, pageNumber, pageSize);
            if (getAll == null || !getAll.Status)
            {
                return View(getAll);
            }
            return View(getAll);
        }
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var customerCheck = await EnsureCustomerOwnsResourceAsync(id);
            if (customerCheck is not null)
            {
                return customerCheck;
            }

            var getCustomer = await _customerService.GetByIdAsync(id, CancellationToken.None);
            if (getCustomer == null || !getCustomer.Status)
            {
                return NotFound();
            }
            return View(getCustomer);
        }
        [HttpPost, ActionName ("Delete")]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute]Guid id)
        {
            var customerCheck = await EnsureCustomerOwnsResourceAsync(id);
            if (customerCheck is not null)
            {
                return customerCheck;
            }

            var deleteCustomer = await _customerService.DeleteAsync(id);
            if (!deleteCustomer.Status)
            {
                return NotFound();
            }
            return RedirectToAction("Logout","User");
        }
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetCustomerById(Guid id)
        {
            var getCustomer = await _customerService.GetByIdAsync(id,CancellationToken.None);
            if (getCustomer == null)
            {
                return NotFound();
            }
            return View(getCustomer);
        }

        private async Task<IActionResult?> EnsureCustomerOwnsResourceAsync(Guid customerId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "User");
            }

            var signedInCustomer = await _customerService.GetByUserIdAsync(userId, CancellationToken.None);
            if (signedInCustomer == null || !signedInCustomer.Status || signedInCustomer.Data == null)
            {
                return NotFound();
            }

            if (signedInCustomer.Data.Id != customerId)
            {
                return Forbid();
            }

            return null;
        }
    }
}

