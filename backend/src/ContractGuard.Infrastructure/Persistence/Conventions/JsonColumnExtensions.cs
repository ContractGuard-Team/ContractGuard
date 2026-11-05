using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractGuard.Infrastructure.Persistence.Conventions;

public static class JsonColumnExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    // Stores a value object as a jsonb column, serialized with System.Text.Json (snake_case keys).
    // The comparer compares the serialized text, so EF detects changes inside lists too.
    public static PropertyBuilder<T> HasJsonColumn<T>(this PropertyBuilder<T> property)
        where T : class
    {
        property
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, JsonOptions),
                json => JsonSerializer.Deserialize<T>(json, JsonOptions)!,
                new ValueComparer<T>(
                    (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
                    value => JsonSerializer.Serialize(value, JsonOptions).GetHashCode(StringComparison.Ordinal),
                    value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!));
        return property;
    }
}
