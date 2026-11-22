using System.ComponentModel.DataAnnotations;

namespace ContractGuard.Infrastructure.Email;

public sealed class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";
    public const string SmtpProvider = "smtp";
    public const string SesProvider = "ses";
    public const string GmailProvider = "gmail";

    // "smtp": Mailpit in development and tests. "ses": Amazon SES through the AWS SDK (SD-16).
    // "gmail": the Gmail API over HTTPS, for Railway staging where SMTP is blocked (docs/04 C5).
    [Required, AllowedValues(SmtpProvider, SesProvider, GmailProvider)]
    public string Provider { get; init; } = SmtpProvider;

    public string SmtpHost { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 25;

    // Gmail SMTP (docs/04 C5 fallback): set both, and the sender switches on STARTTLS. Empty for Mailpit.
    public string? SmtpUsername { get; init; }

    public string? SmtpPassword { get; init; }

    // Gmail API: an OAuth client and the refresh token the sending account granted it (scope gmail.send).
    public string? GmailClientId { get; init; }

    public string? GmailClientSecret { get; init; }

    public string? GmailRefreshToken { get; init; }

    // With SES this address (or its domain) must be a verified identity.
    [Required, EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    // Where links in emails point to: the SPA, not the API.
    [Required, Url]
    public string FrontendBaseUrl { get; init; } = string.Empty;

    public bool UsesSes => Provider == SesProvider;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Provider == SmtpProvider && string.IsNullOrWhiteSpace(SmtpHost))
        {
            yield return new ValidationResult("SmtpHost is required when Provider is smtp.", [nameof(SmtpHost)]);
        }

        if (Provider == GmailProvider && (string.IsNullOrWhiteSpace(GmailClientId) || string.IsNullOrWhiteSpace(GmailClientSecret) || string.IsNullOrWhiteSpace(GmailRefreshToken)))
        {
            yield return new ValidationResult("GmailClientId, GmailClientSecret and GmailRefreshToken are required when Provider is gmail.",
                [nameof(GmailClientId), nameof(GmailClientSecret), nameof(GmailRefreshToken)]);
        }
    }
}
