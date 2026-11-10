using ContractGuard.Application.Analysis;
using ContractGuard.Application.Chat;
using ContractGuard.Application.Contracts;
using ContractGuard.Application.Reports;
using ContractGuard.Application.Workspaces;
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
        services.AddScoped<AnalysisStarter>();
        services.AddScoped<ContractUploadService>();
        services.AddScoped<ContractQueries>();
        services.AddScoped<ContractCommands>();
        services.AddScoped<ReportService>();
        services.AddScoped<ChatService>();
        services.AddScoped<AuditLog>();
        services.AddScoped<ShareService>();
        services.AddScoped<OrganizationService>();

        return services;
    }
}
