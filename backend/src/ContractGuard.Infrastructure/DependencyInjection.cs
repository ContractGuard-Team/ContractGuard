using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContractGuard.Infasttructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfastructure(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            return services;
        }
}