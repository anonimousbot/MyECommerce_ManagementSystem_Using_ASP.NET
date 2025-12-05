using System.Text.Json.Serialization;

namespace EMS.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Status : byte
    {
        Pending=1,
        Processing,
        Delivered,
        Cancelled
    }
}
