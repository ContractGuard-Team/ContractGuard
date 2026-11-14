using System.Security.Cryptography;
using System.Text;

namespace ContractGuard.Domain.Workspaces;

// An invitation of an email to an organization (ENT-DATA-004). Valid 7 days, deleted once accepted or
// expired (OQ-ORG-003). Only a hash of the token is stored, so a database leak gives no usable links.
public sealed class Invitation
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private Invitation()
    {
    }

    public Guid InvitationId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public Guid InvitedByUserAccountId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    // Returns the invitation and the raw token that goes into the email link, once.
    public static (Invitation Invitation, string Token) Create(Guid organizationId, string email, Guid invitedBy, DateTimeOffset now)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var invitation = new Invitation
        {
            InvitationId = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            Email = email.Trim().ToLowerInvariant(),
            InvitedByUserAccountId = invitedBy,
            TokenHash = Hash(token),
            CreatedAt = now,
            ExpiresAt = now + Lifetime
        };
        return (invitation, token);
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
