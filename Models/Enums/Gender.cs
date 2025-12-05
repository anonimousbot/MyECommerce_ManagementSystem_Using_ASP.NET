using System.Text.Json.Serialization;

namespace EMS.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Gender : byte
    {
        Male=1,
        Female,
        Other,
    }
}
