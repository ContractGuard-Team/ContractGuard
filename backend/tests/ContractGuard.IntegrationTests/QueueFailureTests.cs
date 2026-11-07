using System.Net;

namespace ContractGuard.IntegrationTests;

// Switching the queue off affects every job, so these run apart from the other tests of the collection:
// xUnit runs a collection's classes one after another.
[Collection(ApiCollection.Name)]
public sealed class QueueFailureTests(ApiFactory factory)
{
    [Fact]
    public async Task Queue_down_fails_the_run_at_once_and_the_user_can_retry()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, versionId) = await Scenario.UploadContractAsync(factory, client);

            factory.Queue.FailEverything = true;
            try
            {
                await ApiAssert.ProblemAsync(
                    await client.PutAsync($"/contracts/{contractId}/versions/{versionId}/context", Scenario.Context),
                    HttpStatusCode.ServiceUnavailable,
                    "ERR-ANA-001");
            }
            finally
            {
                factory.Queue.FailEverything = false;
            }

            var detail = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}"));
            Assert.Equal("new", detail.GetProperty("approvalStatus").GetString());
            Assert.Equal("failed", detail.GetProperty("latestJob").GetProperty("status").GetString());

            await Scenario.StartAnalysisAsync(client, contractId, versionId);
        }
    }
}
