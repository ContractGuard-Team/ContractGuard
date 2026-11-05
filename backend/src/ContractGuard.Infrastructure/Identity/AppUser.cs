using ContractGuard.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace ContractGuard.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public UiLanguage UiLanguage { get; set; } = UiLanguage.Vi;
}
