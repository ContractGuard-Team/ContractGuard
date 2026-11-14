using ContractGuard.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace ContractGuard.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public const int DisplayNameMaxLength = 100;

    // The name people see. The account is identified by Email; UserName only holds a copy of the
    // email because SignInManager looks accounts up through it.
    public string DisplayName { get; set; } = string.Empty;

    public UiLanguage UiLanguage { get; set; } = UiLanguage.Vi;

    // FR-AUTH-014: when account emails were sent in the last hour (EmailThrottle).
    public List<DateTimeOffset> PasswordResetEmailsSentAt { get; set; } = [];
    public List<DateTimeOffset> VerificationEmailsSentAt { get; set; } = [];
}
