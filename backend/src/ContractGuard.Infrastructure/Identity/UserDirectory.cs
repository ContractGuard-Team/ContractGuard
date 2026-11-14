using ContractGuard.Application.Abstractions;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Infrastructure.Identity;

internal sealed class UserDirectory(AppDbContext db) : IUserDirectory
{
    public Task<UserSummary?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return db.Users
            .Where(u => u.NormalizedEmail == normalized && u.EmailConfirmed)
            .Select(u => new UserSummary(u.Id, u.DisplayName, u.Email!))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Dictionary<Guid, UserSummary>> GetAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        return db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserSummary(u.Id, u.DisplayName, u.Email!))
            .ToDictionaryAsync(u => u.UserId, cancellationToken);
    }
}
