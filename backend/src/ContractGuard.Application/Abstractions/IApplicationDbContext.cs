using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Chat;
using ContractGuard.Domain.Contracts;
using ContractGuard.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Application.Abstractions;

// The database as the Application layer sees it. AppDbContext (Infrastructure) implements it.
// Trade-off: Application depends on the EF Core package (DbSet, LINQ async operators) instead of
// one repository interface per aggregate. For one developer and ~20 tables this saves a lot of
// pass-through code; the cost is that switching ORM would touch Application too.
public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<Membership> Memberships { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ContractVersion> ContractVersions { get; }
    DbSet<AnalysisRun> AnalysisRuns { get; }
    DbSet<Clause> Clauses { get; }
    DbSet<Finding> Findings { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<ContractShare> ContractShares { get; }
    DbSet<AuditEvent> AuditEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Forgets every tracked change (e.g. a half-applied analysis result before marking it failed).
    void DiscardChanges();
}
