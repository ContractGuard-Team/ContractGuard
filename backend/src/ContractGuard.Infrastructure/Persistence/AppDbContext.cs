using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;
using ContractGuard.Domain.Workspaces;
using ContractGuard.Infrastructure.Identity;
using ContractGuard.Infrastructure.Persistence.Conventions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Infrastructure.Persistence;

// IdentityUserContext (not IdentityDbContext) creates the user tables only, without role tables:
// roles in ContractGuard live in membership.org_role and contract_share.contract_role.
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, CurrentWorkspace workspace)
    : IdentityUserContext<AppUser, Guid>(options), IApplicationDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();
    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();
    public DbSet<Clause> Clauses => Set<Clause>();

    public void DiscardChanges() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity configures its own tables first; our configurations then override names.
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_account_claim");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_account_login");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_account_token");

        RenameForeignKeys(modelBuilder);
        ApplyWorkspaceFilters(modelBuilder);
    }

    // SD-05, BR-DATA-001: every query on contract data is limited to the current workspace, whoever
    // writes it. EF reads the fields of "workspace" from the context instance running the query.
    // BR-SHARE-001: in an organization a contract is visible to its owner and to organization owners.
    // Contract shares extend this in P5-02.
    private void ApplyWorkspaceFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contract>().HasQueryFilter(c =>
            workspace.IsSystem
            || (workspace.OrganizationId == null
                && c.OrganizationId == null
                && c.OwnerUserAccountId == workspace.CurrentUserId)
            || (workspace.OrganizationId != null
                && c.OrganizationId == workspace.OrganizationId
                && (workspace.IsOrganizationOwner || c.OwnerUserAccountId == workspace.CurrentUserId)));

        // Child tables follow their parent, so a direct query on them cannot go around the rule.
        modelBuilder.Entity<ContractVersion>().HasQueryFilter(v => Set<Contract>().Any(c => c.ContractId == v.ContractId));
        modelBuilder.Entity<AnalysisRun>().HasQueryFilter(r => Set<Contract>().Any(c => c.ContractId == r.ContractId));
        modelBuilder.Entity<Clause>().HasQueryFilter(c => Set<AnalysisRun>().Any(r => r.AnalysisRunId == c.AnalysisRunId));
    }

    // Configurations run in alphabetical order, so a relationship can be declared before the
    // principal table gets its final name, and EF then names the key after the DbSet
    // ("fk_contract_organizations_..."). Recompute every name from the final table names.
    private static void RenameForeignKeys(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var columns = string.Join('_', foreignKey.Properties.Select(p => p.GetColumnName()));
                foreignKey.SetConstraintName(
                    $"fk_{entityType.GetTableName()}_{foreignKey.PrincipalEntityType.GetTableName()}_{columns}");
            }
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every enum of the Domain assembly is stored as lower snake_case text (SRS §9.3.1.2).
        var enumTypes = typeof(Organization).Assembly.GetTypes().Where(t => t.IsEnum);
        foreach (var enumType in enumTypes)
        {
            var converterType = typeof(SnakeCaseEnumConverter<>).MakeGenericType(enumType);
            configurationBuilder.Properties(enumType).HaveConversion(converterType);
        }
    }
}
