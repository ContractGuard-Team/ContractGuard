using Microsoft.AspNetCore.Identity;

namespace ContractGuard.Infrastructure.Identity;

// FR-AUTH-011: email and Google sign-in share one account per email address, both ways.
// Callers pass only an address Google has verified.
public sealed class GoogleAccounts(UserManager<AppUser> users)
{
    public const string Provider = "Google";

    public async Task<AppUser> FindOrCreateAsync(string providerKey, string email, string? name)
    {
        // Linked before: the account stays the same even if the Google address changed since.
        if (await users.FindByLoginAsync(Provider, providerKey) is { } linked)
        {
            return linked;
        }

        var login = new UserLoginInfo(Provider, providerKey, Provider);
        if (await users.FindByEmailAsync(email) is { } existing)
        {
            if (!existing.EmailConfirmed)
            {
                // Someone else may have registered this address with a password they know, waiting
                // for the owner to link Google (P6-01, docs/04). The owner sets a password through
                // "forgot password" if they want one.
                if (await users.HasPasswordAsync(existing))
                {
                    ThrowIfFailed(await users.RemovePasswordAsync(existing));
                }

                existing.EmailConfirmed = true;
                ThrowIfFailed(await users.UpdateAsync(existing));
            }

            ThrowIfFailed(await users.AddLoginAsync(existing, login));
            return existing;
        }

        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            DisplayName = DisplayNameFor(name, email),
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        ThrowIfFailed(await users.CreateAsync(user));
        ThrowIfFailed(await users.AddLoginAsync(user, login));
        return user;
    }

    // docs/03 Phase 6: the Google name, otherwise the part of the address before "@".
    private static string DisplayNameFor(string? name, string email)
    {
        var at = email.IndexOf('@');
        var displayName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : at > 0 ? email[..at] : email;
        return displayName.Length <= AppUser.DisplayNameMaxLength ? displayName : displayName[..AppUser.DisplayNameMaxLength];
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Google sign-in failed: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }
    }
}
