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

// Secrets of one environment on a developer machine, e.g. the Supabase connection string of
// "Cloud". Git-ignored; user-secrets would leak them into every Development run as well.
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true);

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

// TLS ends before the API (ALB on AWS, Railway's edge on staging), so the API itself only sees http.
// Without the original scheme the antiforgery system refuses every request in Production, where its
// cookie is Secure-only. The client address is two hops back in X-Forwarded-For: the edge appends the
// viewer, the next proxy (CloudFront → ALB, or Railway's edge → nginx) appends the edge, so anything a
// client sends itself sits further left. The lists are cleared because neither hop has a fixed address;
// only that last proxy can reach the API (SD-03, docs/04 C6).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
    options.ForwardLimit = 2;
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

// On AWS the ALB listens on http (no domain, so no certificate; docs/04 C9) and sets X-Forwarded-Proto: http.
// CloudFront overwrites CloudFront-Forwarded-Proto with the viewer's real scheme, and only CloudFront reaches
// the ALB. A client sending it elsewhere can only turn http into https, which makes cookies stricter.
app.Use((context, next) =>
{
    if (context.Request.Headers["CloudFront-Forwarded-Proto"] == "https")
    {
        context.Request.Scheme = "https";
    }

    return next(context);
});

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
