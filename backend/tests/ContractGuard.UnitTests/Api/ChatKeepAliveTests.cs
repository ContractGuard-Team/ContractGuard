using ContractGuard.Api.Endpoints;
using ContractGuard.Application.Chat;

namespace ContractGuard.UnitTests.Api;

public sealed class ChatKeepAliveTests
{
    [Fact]
    public async Task Silence_is_filled_with_pings_and_every_event_still_arrives_in_order()
    {
        var types = new List<string>();
        var cancellationToken = TestContext.Current.CancellationToken;
        await foreach (var item in ChatEndpoints.WithKeepAlive(SlowAnswer(cancellationToken), TimeSpan.FromMilliseconds(20), cancellationToken))
        {
            types.Add(item.EventType!);
        }

        Assert.Equal(["question", "token", "done"], types.Where(t => t != "ping"));
        Assert.Contains("ping", types.Take(types.IndexOf("token")));
    }

    [Fact]
    public async Task Cancelling_while_silent_ends_the_stream()
    {
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in ChatEndpoints.WithKeepAlive(SlowAnswer(cancel.Token), TimeSpan.FromMilliseconds(20), cancel.Token))
            {
            }
        });
    }

    private static async IAsyncEnumerable<ChatEvent> SlowAnswer([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatEvent("question");
        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        yield return new ChatEvent("token", "Điều 4.");
        yield return new ChatEvent("done");
    }
}
