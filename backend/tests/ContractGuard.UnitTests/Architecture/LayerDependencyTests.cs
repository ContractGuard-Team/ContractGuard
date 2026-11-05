using System.Reflection;
using ContractGuard.Domain.Contracts;

namespace ContractGuard.UnitTests.Architecture;

// The dependency rule of Clean Architecture, checked on the compiled assemblies:
// Domain <- Application <- Infrastructure <- Api. A wrong "using" or project reference fails CI.
public sealed class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Contract).Assembly;
    private static readonly Assembly Application = typeof(ContractGuard.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(ContractGuard.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_depends_on_nothing_of_ours_or_on_frameworks()
    {
        AssertNoReference(Domain, "ContractGuard.Application", "ContractGuard.Infrastructure", "ContractGuard.Api",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "AWSSDK", "Npgsql");
    }

    [Fact]
    public void Application_does_not_know_infrastructure_or_web()
    {
        AssertNoReference(Application, "ContractGuard.Infrastructure", "ContractGuard.Api",
            "Microsoft.AspNetCore", "AWSSDK", "Npgsql");
    }

    [Fact]
    public void Infrastructure_does_not_know_the_api()
    {
        AssertNoReference(Infrastructure, "ContractGuard.Api");
    }

    private static void AssertNoReference(Assembly assembly, params string[] forbiddenPrefixes)
    {
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();
        var violations = references.Where(r => forbiddenPrefixes.Any(p => r.StartsWith(p, StringComparison.Ordinal))).ToList();
        Assert.True(violations.Count == 0, $"{assembly.GetName().Name} must not reference: {string.Join(", ", violations)}");
    }
}
