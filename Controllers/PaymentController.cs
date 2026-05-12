using EMS.Interfaces.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using EMS.Models.Options;
using EMS.Models.Security;

namespace EMS.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
    public class PaymentController(IPaymentService paymentService, IOptions<PaystackOptions> paystackOptions) : Controller
    {
        private readonly IPaymentService _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        private readonly PaystackOptions _paystackOptions = paystackOptions?.Value ?? throw new ArgumentNullException(nameof(paystackOptions));

        [HttpGet]
        public async Task<IActionResult> PaystackCallback(string reference)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "User");
            }

            var result = await _paymentService.VerifyPaystackForUserAsync(userId, reference, CancellationToken.None);
            return View(result);
        }

        // Extra security: Paystack webhook with signature verification (recommended).
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaystackWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var signatureHeader = Request.Headers["x-paystack-signature"].ToString();
            if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(_paystackOptions.SecretKey))
            {
                return Unauthorized();
            }

            signatureHeader = signatureHeader.Trim().ToLowerInvariant();
            var computed = ComputePaystackSignature(_paystackOptions.SecretKey, body).ToLowerInvariant();
            if (signatureHeader.Length != computed.Length)
            {
                return Unauthorized();
            }
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(signatureHeader),
                    Encoding.UTF8.GetBytes(computed)))
            {
                return Unauthorized();
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var reference = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("reference")
                    .GetString();

                if (string.IsNullOrWhiteSpace(reference))
                {
                    return Ok();
                }

                await _paymentService.VerifyPaystackAsync(reference, CancellationToken.None);
                return Ok();
            }
            catch
            {
                return Ok();
            }
        }

        private static string ComputePaystackSignature(string secretKey, string payload)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
