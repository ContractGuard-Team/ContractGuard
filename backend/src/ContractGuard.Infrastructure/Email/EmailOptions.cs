using System.ComponentModel.DataAnnotations;

namespace ContractGuard.Infrastructure.Email;

public sealed class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";
    public const string SmtpProvider = "smtp";
    public const string SesProvider = "ses";

    // "smtp": Mailpit in development and tests. "ses": Amazon SES through the AWS SDK (SD-16).
    [Required, AllowedValues(SmtpProvider, SesProvider)]
    public string Provider { get; init; } = SmtpProvider;

    public string SmtpHost { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 25;

    // Gmail SMTP (docs/04 C5 fallback): set both, and the sender switches on STARTTLS. Empty for Mailpit.
    public string? SmtpUsername { get; init; }

    public string? SmtpPassword { get; init; }

    // With SES this address (or its domain) must be a verified identity.
    [Required, EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    // Where links in emails point to: the SPA, not the API.
    [Required, Url]
    public string FrontendBaseUrl { get; init; } = string.Empty;

    public bool UsesSes => Provider == SesProvider;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!UsesSes && string.IsNullOrWhiteSpace(SmtpHost))
        {
            yield return new ValidationResult("SmtpHost is required when Provider is smtp.", [nameof(SmtpHost)]);
        }
    }
}
