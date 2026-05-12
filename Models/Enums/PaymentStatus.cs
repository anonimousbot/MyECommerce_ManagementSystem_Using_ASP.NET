using System.Text.Json.Serialization;

namespace EMS.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus : byte
    {
        Initialized = 1,
        Successful,
        Failed,
        SuccessfulButUnfulfilled
    }
}
