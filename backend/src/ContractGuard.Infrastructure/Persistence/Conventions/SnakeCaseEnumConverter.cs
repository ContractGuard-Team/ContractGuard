using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ContractGuard.Infrastructure.Persistence.Conventions;

// Stores enums as the lower snake_case, e.g. PendingReview <-> "pending_review".
public sealed class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()),
    value => Enum.Parse<TEnum>(value.Replace("_", string.Empty), true))
    where TEnum : struct, Enum;
