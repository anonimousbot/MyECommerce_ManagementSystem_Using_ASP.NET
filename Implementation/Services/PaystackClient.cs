using System.Net.Http.Headers;
using System.Net.Http.Json;
using EMS.Interfaces.Services;
using EMS.Models.Options;
using Microsoft.Extensions.Options;

namespace EMS.Implementation.Services
{
    public class PaystackClient : IPaystackClient
    {
        private readonly HttpClient _httpClient;
        private readonly PaystackOptions _options;

        public PaystackClient(HttpClient httpClient, IOptions<PaystackOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<(bool Ok, string Message, string? AuthorizationUrl, string? Reference)> InitializeAsync(
            string email,
            int amountKobo,
            string callbackUrl,
            string? reference,
            object metadata,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return (false, "Paystack secret key is not configured.", null, null);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "transaction/initialize");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

            request.Content = JsonContent.Create(new
            {
                email,
                amount = amountKobo,
                reference,
                callback_url = callbackUrl,
                metadata
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<PaystackInitResponse>(cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode || payload == null || payload.status == false || payload.data == null)
            {
                return (false, payload?.message ?? $"Paystack init failed ({(int)response.StatusCode}).", null, null);
            }

            return (true, payload.message ?? "Initialized", payload.data.authorization_url, payload.data.reference);
        }

        public async Task<(bool Ok, string Message, bool Paid, int? AmountKobo, string? Currency)> VerifyAsync(string reference, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return (false, "Paystack secret key is not configured.", false, null, null);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"transaction/verify/{Uri.EscapeDataString(reference)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<PaystackVerifyResponse>(cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode || payload == null || payload.status == false || payload.data == null)
            {
                return (false, payload?.message ?? $"Paystack verify failed ({(int)response.StatusCode}).", false, null, null);
            }

            var paid = string.Equals(payload.data.status, "success", StringComparison.OrdinalIgnoreCase);
            return (true, payload.message ?? "Verified", paid, payload.data.amount, payload.data.currency);
        }

        private sealed class PaystackInitResponse
        {
            public bool status { get; set; }
            public string? message { get; set; }
            public PaystackInitData? data { get; set; }
        }

        private sealed class PaystackInitData
        {
            public string? authorization_url { get; set; }
            public string? reference { get; set; }
        }

        private sealed class PaystackVerifyResponse
        {
            public bool status { get; set; }
            public string? message { get; set; }
            public PaystackVerifyData? data { get; set; }
        }

        private sealed class PaystackVerifyData
        {
            public string? status { get; set; }
            public int? amount { get; set; }
            public string? currency { get; set; }
        }
    }
}
