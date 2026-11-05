using ContractGuard.Application.Analysis;
using Microsoft.Extensions.DependencyInjection;

namespace ContractGuard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped: one instance per HTTP request or per queue message, sharing that scope's DbContext.
        services.AddScoped<AnalysisResultHandler>();
        services.AddScoped<AnalysisResultProcessor>();
        services.AddScoped<StaleRunSweeper>();

        return services;
    }
}
