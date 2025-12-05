using System.Security.Claims;
using EMS.Interfaces.Services;
using EMS.Models.DTOs.Customers;
using EMS.Models.Enums;
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

        [Authorize (Roles = "Customer")]
        public async Task<IActionResult> Index(string searchString)
        {
            var items = await _itemService.GetItemAsync(CancellationToken.None);
            // SearchBar Code 
            if (!String.IsNullOrEmpty(searchString))
            {
                items.Data = items.Data.Where(n => n.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                   .ToList();
                return View(items);
            }
            //else if(brand != null)
            //{
            //    items.Data = items.Data.Where(b => b.Brand.Equals(brand.ToString(), StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //    return View(items);
            //}
             return View(items);
           
            
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]    
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
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update(Guid id)
        {
            var getCustomer = await _customerService.GetByIdAsync(id, CancellationToken.None);
            if (getCustomer == null)
            {
                return NotFound();
            }
            return View();
        }
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update(UpdateCustomerRequestModel model,Guid id)
        {
            var updateCustomer = await _customerService.UpdateAsync(model, id);
            if (updateCustomer == null)
            {
                return NotFound();
            }
            return RedirectToAction("CustomerProfile","Customer");
        }

        [HttpGet]
        [Authorize(Roles = "Customer")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCustomer()
        {
            var getAll = await _customerService.GetCustomerAsync(CancellationToken.None);
            if (getAll == null || !getAll.Status)
            {
                return NotFound();
            }
            return View(getAll);
        }
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var getCustomer = await _customerService.GetByIdAsync(id, CancellationToken.None);
            if (getCustomer == null)
            {
                return NotFound();
            }
            return View(getCustomer);
        }
        [HttpPost, ActionName ("Delete")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteConfirmed([FromRoute]Guid id)
        {
            var deleteCustomer = await _customerService.DeleteAsync(id);
            if (!deleteCustomer.Status)
            {
                return NotFound();
            }
            return RedirectToAction("Logout","User");
        }
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> GetCustomerById(Guid id)
        {
            var getCustomer = await _customerService.GetByIdAsync(id,CancellationToken.None);
            if (getCustomer == null)
            {
                return NotFound();
            }
            return View(getCustomer);
        }

    }
}

