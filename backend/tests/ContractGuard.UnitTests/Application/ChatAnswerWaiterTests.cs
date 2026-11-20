using ContractGuard.Application.Chat;

namespace ContractGuard.UnitTests.Application;

public sealed class ChatAnswerWaiterTests
{
    private readonly ChatAnswerWaiter _waiter = new();

    [Fact]
    public async Task Answer_reaches_the_waiting_question_once()
    {
        var jobId = Guid.CreateVersion7();
        var waiting = _waiter.Register(jobId);

        Assert.True(_waiter.TryComplete(jobId, new WorkerChatAnswer("Điều 4.", ["cl-004"])));
        Assert.Equal("Điều 4.", (await waiting).Text);
        Assert.False(_waiter.TryComplete(jobId, new WorkerChatAnswer("again", []))); // a redelivered message
    }

    [Fact]
    public async Task Failure_ends_the_waiting_question_with_an_error()
    {
        var jobId = Guid.CreateVersion7();
        var waiting = _waiter.Register(jobId);

        Assert.True(_waiter.TryFail(jobId, "model down"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => waiting);
    }

    [Fact]
    public void Nobody_waits_after_timeout_or_for_an_unknown_id()
    {
        var jobId = Guid.CreateVersion7();
        _waiter.Register(jobId);
        _waiter.Remove(jobId);

        Assert.False(_waiter.TryComplete(jobId, new WorkerChatAnswer("late", [])));
        Assert.False(_waiter.TryFail(Guid.CreateVersion7(), "unknown"));
    }
}
