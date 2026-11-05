using ContractGuard.Application.Analysis;
using ContractGuard.Domain.Analysis;
using ContractGuard.Domain.Contracts;
using ContractGuard.Infrastructure.Identity;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContractGuard.Api.Endpoints;

// Endpoints for local testing only. Program.cs maps them in the Development environment,
// so they do not exist on staging or production.
internal static class DevEndpoints
{
    private const string DevEmail = "dev@contractguard.local";

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

        // Creates a dev user, a personal contract with a confirmed context and a running analysis,
        // without uploading a file or calling the worker. Lets scripts/dev/send-analysis-result.ps1
        // play the worker's part before the upload flow exists.
        dev.MapPost("/seed-analysis", async (AppDbContext db, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            var now = clock.GetUtcNow();
            var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == DevEmail.ToUpperInvariant(), cancellationToken);
            if (user is null)
            {
                user = new AppUser
                {
                    Id = Guid.CreateVersion7(),
                    UserName = DevEmail,
                    NormalizedUserName = DevEmail.ToUpperInvariant(),
                    Email = DevEmail,
                    NormalizedEmail = DevEmail.ToUpperInvariant(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                db.Users.Add(user);
            }

            var contract = Contract.Create(
                "Cloud Service Agreement (dev seed)",
                organizationId: null,
                user.Id,
                new ContractFile($"quarantine/{Guid.CreateVersion7()}.pdf", "cloud-service-agreement.pdf", FileType.Pdf, 250_000),
                now);
            var version = contract.LatestVersion;
            contract.ConfirmContext(
                version.ContractVersionId,
                new ContextDetails(OwnParty.Buyer, ContractType.ServiceAgreement, HasPersonalData: true, "Example Cloud", "Cloud hosting", ServiceCriticality.High),
                now);
            contract.MarkAnalysisStarted(version.ContractVersionId, now);
            var run = AnalysisRun.Start(contract.ContractId, version.ContractVersionId, now);

            db.Contracts.Add(contract);
            db.AnalysisRuns.Add(run);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { job_id = run.AnalysisRunId, contract_id = contract.ContractId, contract_version = version.VersionNumber });
        });

        return app;
    }
}
