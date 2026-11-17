using System.Text.Json;
using System.Text.Json.Serialization;
using ContractGuard.Api.Endpoints;
using ContractGuard.Api.Health;
using ContractGuard.Api.Http;
using ContractGuard.Application;
using ContractGuard.Domain.Common;
using ContractGuard.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction() && string.Equals(builder.Configuration["Chat:Responder"], "fake", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Chat:Responder=fake is for development and tests only.");
}

builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddApiAuth(builder.Configuration, builder.Environment);
builder.Services.AddScoped<AccountEmails>();
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Enums travel as "service_agreement", same as in the database. Numbers are refused:
    // otherwise 99 would be accepted as an enum value that does not exist.
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));

    // A missing or null field that the request type marks as required is a 400,
    // instead of reaching the endpoint as null.
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});

// TLS ends at the load balancer, so the API itself only sees http. Without the original scheme the
// antiforgery system refuses every request in Production, where its cookie is Secure-only.
// The lists are cleared because the load balancer has no fixed address; only it can reach the API (SD-03).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddApiRateLimits(builder.Configuration);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // Responses without a specific error (no route, no session, malformed body) get a code too,
    // so the frontend always has one to react to.
    var code = context.ProblemDetails.Status switch
    {
        StatusCodes.Status400BadRequest => ErrorCodes.Validation,
        StatusCodes.Status401Unauthorized => ErrorCodes.Unauthenticated,
        StatusCodes.Status403Forbidden => ErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => ErrorCodes.NotFound,
        _ => null
    };
    if (code is not null)
    {
        context.ProblemDetails.Extensions.TryAdd("code", code);
    }
});

var app = builder.Build();

// `dotnet run -- seed` (or `dotnet ContractGuard.Api.dll seed` in the container): demo data, then exit.
if (args.Contains("seed"))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("The seed is for the Development environment only.");
    }

    await DevSeeder.RunAsync(app.Services, app.Logger);
    return;
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

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

var api = app.MapGroup("/api/v1").AddEndpointFilter<CsrfEndpointFilter>();
api.MapPublicAuthEndpoints();
api.MapPublicInvitationEndpoints();

var secured = api.MapGroup(string.Empty)
    .RequireAuthorization()
    .AddEndpointFilter<WorkspaceEndpointFilter>();
secured.MapAccountEndpoints();
secured.MapContractEndpoints();
secured.MapChatEndpoints();
secured.MapOrganizationEndpoints();

app.Run();
