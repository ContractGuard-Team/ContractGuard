using System.ComponentModel.DataAnnotations;

namespace ContractGuard.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public string SmtpHost { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 25;

    [Required, EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    // Where links in emails point to: the SPA, not the API.
    [Required, Url]
    public string FrontendBaseUrl { get; init; } = string.Empty;
}
