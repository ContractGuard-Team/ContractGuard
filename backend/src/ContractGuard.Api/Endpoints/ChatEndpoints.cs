using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using ContractGuard.Application.Chat;
using ContractGuard.Domain.Common;
using ContractGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace ContractGuard.Api.Endpoints;

// F-CHAT-001, F-CHAT-003: the contract's shared conversation, and a question answered over SSE (SD-15).
// POST, not GET with EventSource: the question is a body and the request needs the CSRF header.
internal static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder secured)
    {
        var chat = secured.MapGroup("/contracts/{contractId:guid}/chat").WithTags("Chat");
        chat.MapGet("/", GetThread);
        chat.MapPost("/", Ask).Produces(StatusCodes.Status200OK, contentType: "text/event-stream");
        return secured;
    }

    private static async Task<Ok<ChatThread>> GetThread(Guid contractId, ChatService chat, CancellationToken cancellationToken) =>
        TypedResults.Ok(await chat.GetThreadAsync(contractId, cancellationToken));

    // CloudFront drops an origin that stays silent for 30s (the ALB after 60s), and the worker answers in one
    // go, often later than that (docs/04 C6). A "ping" event in between keeps the stream open; the web ignores it.
    internal static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(15);

    private static async Task<ServerSentEventsResult<ChatEvent>> Ask(
        Guid contractId,
        AskRequest request,
        ClaimsPrincipal principal,
        UserManager<AppUser> users,
        ChatService chat,
        CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(principal)
            ?? throw new DomainException(ErrorCodes.Unauthenticated, "The account no longer exists.");
        var prepared = await chat.PrepareAsync(contractId, request.ContractVersionId, request.Question, user.DisplayName, user.UiLanguage, cancellationToken);
        return TypedResults.ServerSentEvents(WithKeepAlive(chat.StreamAnswerAsync(prepared, cancellationToken), KeepAliveInterval, cancellationToken));
    }

    internal static async IAsyncEnumerable<SseItem<ChatEvent>> WithKeepAlive(IAsyncEnumerable<ChatEvent> events, TimeSpan interval, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var items = events.GetAsyncEnumerator(cancellationToken);
        var next = items.MoveNextAsync().AsTask();
        while (true)
        {
            var silence = Task.Delay(interval, cancellationToken);
            // On cancellation, fall through and await the stream too: disposing it mid-move would throw.
            if (await Task.WhenAny(next, silence) == silence && !cancellationToken.IsCancellationRequested)
            {
                yield return new SseItem<ChatEvent>(new ChatEvent("ping"), "ping");
                continue;
            }

            if (!await next)
            {
                yield break;
            }

            yield return new SseItem<ChatEvent>(items.Current, items.Current.Type);
            next = items.MoveNextAsync().AsTask();
        }
    }
}

internal sealed record AskRequest(string Question, Guid? ContractVersionId = null);
