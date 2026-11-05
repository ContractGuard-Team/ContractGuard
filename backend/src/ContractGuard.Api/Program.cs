using ContractGuard.Api.Endpoints;
using ContractGuard.Api.Health;
using ContractGuard.Application;
using ContractGuard.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevEndpoints();
}

// Liveness: the process answers. No dependency is checked, so a database outage
// does not make the orchestrator restart a healthy container.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: every dependency tagged "ready" (Postgres, S3, SQS) is reachable.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync
});

app.Run();
