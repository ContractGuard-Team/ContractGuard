using Microsoft.AspNetCore.Identity;

namespace ContractGuard.Infrastructure.Identity;

// FR-AUTH-005 asks for a letter of any case; Identity only has separate upper and lower case options.
public sealed class PasswordHasLetterValidator : IPasswordValidator<AppUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        return Task.FromResult(password is not null && password.Any(char.IsLetter)
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError { Code = "PasswordRequiresLetter", Description = "The password must contain a letter." }));
    }
}
