namespace ContractGuard.Application.Abstractions;

// Names of the query filters on contracts, so a query can switch off one of them only
// (e.g. IgnoreQueryFilters([QueryFilters.NotDeleted]) still applies the workspace rule).
public static class QueryFilters
{
    public const string Workspace = "Workspace";
    public const string NotDeleted = "NotDeleted";
}
