using System.Security.Claims;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Users;
using EMS.Models.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
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
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
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
                var roleNames = PostLoginNavigation.BuildRoleSet(result.Data.Roles.Select(role => role.Name));

                if (roleNames.Count == 0)
                {
                    ViewBag.ErrorMessage = "Your account has no role assigned. Please contact support.";
                    return RedirectToAction("Login");
                }

                var userClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,string.IsNullOrWhiteSpace(result.Data.FirstName) ? result.Data.Email : result.Data.FirstName),
                    new Claim(ClaimTypes.GivenName,result.Data.FullName),
                     new Claim(ClaimTypes.Email,result.Data.Email),
                    new Claim(ClaimTypes.NameIdentifier,result.Data.UserId.ToString()),
                };
                foreach (var roleName in roleNames)
                {
                    userClaims.Add(new Claim(ClaimTypes.Role, roleName));
                }
                var claimsIdentity=new ClaimsIdentity(userClaims,CookieAuthenticationDefaults.AuthenticationScheme);
                var authenticationProperties = new AuthenticationProperties();
                var principal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, authenticationProperties);
                var destination = PostLoginNavigation.Decide(returnUrl, roleNames, Url.IsLocalUrl);
                return RedirectToDestination(destination);
            }
            ViewBag.ErrorMessage = result.Message;
            return RedirectToAction("Login");
        }        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            var loginResponse = await _userService.LoginAsync(model, CancellationToken.None);
            if (loginResponse.Status)
            {
                var roleNames = PostLoginNavigation.BuildRoleSet(loginResponse.Data.Roles.Select(role => role.Name));
                if (roleNames.Count == 0)
                {
                    ViewBag.ErrorMessage = "Your account has no role assigned. Please contact support.";
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, loginResponse.Data.FirstName),
                    new Claim(ClaimTypes.GivenName, loginResponse.Data.FullName),
                    new Claim(ClaimTypes.Email, loginResponse.Data.Email),
                    new Claim(ClaimTypes.NameIdentifier, loginResponse.Data.UserId.ToString()),
                };
                foreach (var roleName in roleNames)
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleName));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authenticationProperties = new AuthenticationProperties();
                var principal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, authenticationProperties);
                var destination = PostLoginNavigation.Decide(returnUrl, roleNames, Url.IsLocalUrl);
                return RedirectToDestination(destination);
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

        private IActionResult RedirectToDestination(PostLoginDestination destination)
        {
            if (destination.Kind == PostLoginDestinationKind.LocalReturnUrl && !string.IsNullOrWhiteSpace(destination.ReturnUrl))
            {
                return Redirect(destination.ReturnUrl);
            }

            if (destination.Kind == PostLoginDestinationKind.AdminHome)
            {
                return RedirectToAction("Index", "Admin");
            }

            if (destination.Kind == PostLoginDestinationKind.CustomerHome)
            {
                return RedirectToAction("Index", "Customer");
            }

            return RedirectToAction("Login");
        }
    }
}
