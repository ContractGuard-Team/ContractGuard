using System.Text.Json;

namespace ContractGuard.Infrastructure.Messaging;

// JSON settings shared by everything that crosses the C#/Python boundary:
// snake_case keys and enum values (Python convention), nulls omitted.
internal static class MessageJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };
}
