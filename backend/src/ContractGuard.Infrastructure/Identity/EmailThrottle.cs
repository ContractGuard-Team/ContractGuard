namespace ContractGuard.Infrastructure.Identity;

// FR-AUTH-014: per account, no account email within 3 minutes of the previous one, at most 5 per hour.
// The caller answers the same either way, so nobody learns whether an email went out.
public static class EmailThrottle
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(3);
    public const int MaxPerHour = 5;

    // Returns the trimmed history with now added, or null when this send must be skipped.
    public static List<DateTimeOffset>? TryRecord(IEnumerable<DateTimeOffset> sentAt, DateTimeOffset now)
    {
        var lastHour = sentAt.Where(t => t > now - TimeSpan.FromHours(1)).ToList();
        if (lastHour.Count >= MaxPerHour || lastHour.Any(t => t > now - Cooldown))
        {
            return null;
        }

        return [.. lastHour, now];
    }
}
