using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ContractGuard.Application.Analysis;
using ContractGuard.Application.Chat;
using ContractGuard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ContractGuard.IntegrationTests;

// F-CHAT-001, F-CHAT-003: the send/receive frame of the chat, with stand-in models (no AI yet).
[Collection(ApiCollection.Name)]
public sealed class ChatTests(ApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Without_a_model_the_chat_says_it_is_not_connected()
    {
        var (client, _, _) = await Scenario.SignedInUserAsync(factory);
        using (client)
        {
            var (contractId, _, _) = await Scenario.AnalyzedContractAsync(factory, client);

            var thread = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}/chat"));
            Assert.False(thread.GetProperty("available").GetBoolean());
            await ApiAssert.ProblemAsync(await client.PostAsync($"/contracts/{contractId}/chat", new { question = "Ai chịu trách nhiệm?" }), HttpStatusCode.ServiceUnavailable, "ERR-CHAT-001");
        }
    }

    [Fact]
    public async Task Answer_streams_with_citation_and_both_messages_are_kept()
    {
        using var client = factory.CreateApiClientWithSetting("Chat:Responder", "fake");
        await Scenario.SignedInUserAsync(factory, client);
        var (contractId, _, _) = await Scenario.AnalyzedContractAsync(factory, client);

        var response = await client.PostAsync($"/contracts/{contractId}/chat", new { question = "What about the liability for security incidents?" });

        ApiAssert.Status(response, HttpStatusCode.OK);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var stream = await response.Content.ReadAsStringAsync(Ct);
        Assert.StartsWith("event: question", stream, StringComparison.Ordinal);
        Assert.Contains("event: token", stream, StringComparison.Ordinal);
        Assert.Contains("event: citation", stream, StringComparison.Ordinal);
        Assert.Contains("event: done", stream, StringComparison.Ordinal);

        var thread = await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}/chat"));
        Assert.True(thread.GetProperty("available").GetBoolean());
        var messages = thread.GetProperty("messages");
        Assert.Equal(2, messages.GetArrayLength());
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.Equal(Scenario.DisplayName, messages[0].GetProperty("authorName").GetString());
        Assert.Equal("assistant", messages[1].GetProperty("role").GetString());
        Assert.Equal("cl-004", messages[1].GetProperty("citations")[0].GetProperty("clauseKey").GetString());
    }

    [Fact]
    public async Task Model_failure_mid_answer_sends_an_error_event_and_keeps_no_half_answer()
    {
        using var client = factory.CreateApiClientWithServices(services => services.AddSingleton<IChatResponder, BrokenResponder>());
        await Scenario.SignedInUserAsync(factory, client);
        var (contractId, _, _) = await Scenario.AnalyzedContractAsync(factory, client);

        var stream = await (await client.PostAsync($"/contracts/{contractId}/chat", new { question = "Hello?" })).Content.ReadAsStringAsync(Ct);

        Assert.Contains("event: error", stream, StringComparison.Ordinal);
        Assert.Contains("ERR-CHAT-001", stream, StringComparison.Ordinal);
        Assert.DoesNotContain("event: citation", stream, StringComparison.Ordinal); // cl-unknown is dropped
        var messages = (await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}/chat"))).GetProperty("messages");
        Assert.Equal(1, messages.GetArrayLength());
    }

    [Fact]
    public async Task Questions_are_refused_before_analysis_when_empty_or_on_a_foreign_contract()
    {
        using var alice = factory.CreateApiClientWithSetting("Chat:Responder", "fake");
        await Scenario.SignedInUserAsync(factory, alice);
        var (bob, _, _) = await Scenario.SignedInUserAsync(factory);
        using (bob)
        {
            var (contractId, _) = await Scenario.UploadContractAsync(factory, alice);
            await ApiAssert.ProblemAsync(await alice.PostAsync($"/contracts/{contractId}/chat", new { question = "Hi" }), HttpStatusCode.ServiceUnavailable, "ERR-CHAT-001");
            await ApiAssert.ProblemAsync(await alice.PostAsync($"/contracts/{contractId}/chat", new { question = "   " }), HttpStatusCode.BadRequest, "ERR-GEN-VALIDATION");

            // FR-CHAT-006: another user's contract does not exist for the chat either.
            await ApiAssert.ProblemAsync(await bob.GetAsync($"/contracts/{contractId}/chat"), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
            await ApiAssert.ProblemAsync(await bob.PostAsync($"/contracts/{contractId}/chat", new { question = "Hi" }), HttpStatusCode.NotFound, "ERR-GEN-NOT-FOUND");
        }
    }

    [Fact]
    public async Task Worker_answer_streams_with_only_citations_of_the_run()
    {
        await using var app = WorkerChatApp();
        using var client = new ApiClient(app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }));
        await Scenario.SignedInUserAsync(factory, client);
        var (contractId, _, _) = await Scenario.AnalyzedContractAsync(factory, client);
        var before = factory.Queue.Published.Select(m => m.JobId).ToHashSet();

        var post = client.PostAsync($"/contracts/{contractId}/chat", new { question = "Ai chịu trách nhiệm khi có sự cố?" });
        var question = await PublishedChatQuestionAsync(before);

        // The request file carries only this run's data, in the shape of contracts/chat-request.v1.schema.json.
        using var request = JsonDocument.Parse(factory.Storage.GetText(question.RequestKey!));
        Assert.Equal("Ai chịu trách nhiệm khi có sự cố?", request.RootElement.GetProperty("question").GetString());
        Assert.Contains(request.RootElement.GetProperty("clauses").EnumerateArray(), c => c.GetProperty("clause_id").GetString() == "cl-004");
        Assert.True(request.RootElement.GetProperty("findings").GetArrayLength() > 0);

        Assert.Equal(ResultOutcome.Applied, await HandleAsync(app, new AnalysisResultMessage(
            AnalysisEventTypes.ChatAnswered, question.JobId, null, null, null, null, "Điều 4 quy định trách nhiệm.", ["cl-004", "cl-unknown"])));

        var stream = await (await post).Content.ReadAsStringAsync(Ct);
        Assert.Contains("event: token", stream, StringComparison.Ordinal);
        Assert.Contains("cl-004", stream, StringComparison.Ordinal);
        Assert.DoesNotContain("cl-unknown", stream, StringComparison.Ordinal); // not a clause of the run
        Assert.Contains("event: done", stream, StringComparison.Ordinal);
        var messages = (await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}/chat"))).GetProperty("messages");
        Assert.Equal("Điều 4 quy định trách nhiệm.", messages[1].GetProperty("content").GetString()?.Trim());

        // A late duplicate finds nobody waiting and is dropped.
        Assert.Equal(ResultOutcome.Ignored, await HandleAsync(app, new AnalysisResultMessage(
            AnalysisEventTypes.ChatAnswered, question.JobId, null, null, null, null, "again", [])));
    }

    [Fact]
    public async Task Worker_failure_sends_an_error_event_and_keeps_no_answer()
    {
        await using var app = WorkerChatApp();
        using var client = new ApiClient(app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }));
        await Scenario.SignedInUserAsync(factory, client);
        var (contractId, _, _) = await Scenario.AnalyzedContractAsync(factory, client);
        var before = factory.Queue.Published.Select(m => m.JobId).ToHashSet();

        var post = client.PostAsync($"/contracts/{contractId}/chat", new { question = "Hello?" });
        var question = await PublishedChatQuestionAsync(before);
        Assert.Equal(ResultOutcome.Applied, await HandleAsync(app, new AnalysisResultMessage(
            AnalysisEventTypes.Failed, question.JobId, null, null, "model down", null)));

        var stream = await (await post).Content.ReadAsStringAsync(Ct);
        Assert.Contains("ERR-CHAT-001", stream, StringComparison.Ordinal);
        var messages = (await ApiAssert.JsonAsync(await client.GetAsync($"/contracts/{contractId}/chat"))).GetProperty("messages");
        Assert.Equal(1, messages.GetArrayLength());
    }

    // Its own host, so the answer goes through that host's ChatAnswerWaiter; queue and storage are shared.
    private WebApplicationFactory<Program> WorkerChatApp() => factory.WithWebHostBuilder(b => b.UseSetting("Chat:Responder", "worker"));

    private async Task<AnalysisRequestMessage> PublishedChatQuestionAsync(HashSet<Guid> before)
    {
        for (var i = 0; i < 100; i++)
        {
            var question = factory.Queue.Published.FirstOrDefault(m => m.EventType == AnalysisEventTypes.ChatRequested && !before.Contains(m.JobId));
            if (question is not null)
            {
                return question;
            }

            await Task.Delay(50, Ct);
        }

        throw new TimeoutException("No chat_requested was published.");
    }

    private static async Task<ResultOutcome> HandleAsync(WebApplicationFactory<Program> app, AnalysisResultMessage message)
    {
        await using var scope = app.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CurrentWorkspace>().RunAsSystem();
        return await scope.ServiceProvider.GetRequiredService<AnalysisResultHandler>().HandleAsync(message, Ct);
    }

    private sealed class BrokenResponder : IChatResponder
    {
        public bool IsAvailable => true;

        public async IAsyncEnumerable<ChatChunk> AnswerAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return ChatChunk.Of("Một phần ");
            yield return ChatChunk.Cite("cl-unknown");
            await Task.Yield();
            throw new TimeoutException("model timed out");
        }
    }
}
