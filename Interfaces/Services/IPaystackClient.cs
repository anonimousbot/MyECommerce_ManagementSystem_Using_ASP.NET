namespace EMS.Interfaces.Services
{
    public interface IPaystackClient
    {
        Task<(bool Ok, string Message, string? AuthorizationUrl, string? Reference)> InitializeAsync(
            string email,
            int amountKobo,
            string callbackUrl,
            string? reference,
            object metadata,
            CancellationToken cancellationToken);

        Task<(bool Ok, string Message, bool Paid, int? AmountKobo, string? Currency)> VerifyAsync(string reference, CancellationToken cancellationToken);
    }
}
