using System.Security.Claims;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Controllers
{
    public class UserController : Controller
    {
        private IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
       public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(GoogleResponse), "User",new {returnUrl});
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);

        }
        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if(!authenticateResult.Succeeded)
            {
                ViewBag.ErrorMessage = "Google authentication failed";
                return RedirectToAction("Login");
            }
            var claims = authenticateResult.Principal.Identities.FirstOrDefault()?.Claims;

            var googleUserDto = new GoogleUserDTO
            {
                Email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                FullName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                GoogleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
            };
            if (string.IsNullOrEmpty(googleUserDto.Email))
            {
                ViewBag.ErrorMessage = "Failed to retrieve user information from google";
                return RedirectToAction("Login");
            }
            var result = await _userService.GoogleLoginOrRegisterAsync(googleUserDto, CancellationToken.None);
            if(result.Status)
            {
                var userClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,result.Data.FirstName),
                    new Claim(ClaimTypes.GivenName,result.Data.FullName),
                     new Claim(ClaimTypes.Email,result.Data.Email),
                    new Claim(ClaimTypes.NameIdentifier,result.Data.UserId.ToString()),
                };
                foreach (var role in result.Data.Roles)
                {
                    userClaims.Add(new Claim(ClaimTypes.Role, role.Name));
                }
                var claimsIdentity=new ClaimsIdentity(userClaims,CookieAuthenticationDefaults.AuthenticationScheme);
                var authenticationProperties = new AuthenticationProperties();
                var principal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, authenticationProperties);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Customer");
            }
            ViewBag.ErrorMessage = result.Message;
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestModel model)
        {
            var loginResponse = await _userService.LoginAsync(model, CancellationToken.None);
            var checkRole = "";
            if (loginResponse.Status)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, loginResponse.Data.FirstName),
                    new Claim(ClaimTypes.GivenName, loginResponse.Data.FullName),
                    new Claim(ClaimTypes.Email, loginResponse.Data.Email),
                    new Claim(ClaimTypes.NameIdentifier, loginResponse.Data.UserId.ToString()),
                };
                foreach (var role in loginResponse.Data.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Name));
                    checkRole = role.Name;
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authenticationProperties = new AuthenticationProperties();
                var principal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, authenticationProperties);
                if (checkRole == "Customer")
                {
                    return RedirectToAction("Index", "Customer");
                }
                return RedirectToAction("Index", "Item");
            }
            else
            {
                ViewBag.ErrorMessage = loginResponse.Message;
                return View(model);
            }
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("UserId");

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return RedirectToAction("Login", "User");
        }


    }
}
