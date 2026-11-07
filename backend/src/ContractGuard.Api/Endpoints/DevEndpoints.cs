using ContractGuard.Application.Analysis;

namespace ContractGuard.Api.Endpoints;

// Endpoints for local testing only. Program.cs maps them in the Development environment,
// so they do not exist on staging or production.
internal static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var dev = app.MapGroup("/dev").WithTags("Dev").ExcludeFromDescription();

        // Puts a "ping" message on analysis-requests. A worker that is running answers with
        // a "pong" on analysis-results; the results consumer logs it.
        dev.MapPost("/ping-worker", async (IAnalysisJobQueue queue, CancellationToken cancellationToken) =>
        {
            var jobId = Guid.CreateVersion7();
            await queue.PublishAsync(new AnalysisRequestMessage(AnalysisEventTypes.Ping, jobId, null), cancellationToken);
            return Results.Accepted(value: new { job_id = jobId });
        });

        return app;
    }
}
