namespace ContractGuard.Application.Abstractions;

// Read access to user accounts for the Application layer; the accounts themselves live in ASP.NET
// Core Identity (Infrastructure).
public interface IUserDirectory
{
    // Only accounts with a confirmed email: sharing with an unverified address would hand the contract
    // to whoever registered it first.
    Task<UserSummary?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<Dictionary<Guid, UserSummary>> GetAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken);
}
