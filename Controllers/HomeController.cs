using System.Diagnostics;
using EMS.Interfaces.Services;
using EMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IItemService _itemService;

        public HomeController(ILogger<HomeController> logger, IItemService itemService)
        {
            _logger = logger;
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Store));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Store(string? q, int pageNumber = 1, int pageSize = 12)
        {
            var items = await _itemService.GetItemAsync(CancellationToken.None, pageNumber, pageSize, q);
            ViewBag.Query = q;
            return View(items);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            var item = await _itemService.GetByIdAsync(id, CancellationToken.None);
            if (item == null || !item.Status || item.Data == null)
            {
                return NotFound();
            }
            return View(item);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
