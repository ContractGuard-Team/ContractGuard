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

    // shortcut: no keep-alive comments while the model is silent; add one every 15s if the real model
    // can stay silent longer than the load balancer idle timeout (60s).
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
        return TypedResults.ServerSentEvents(Events(chat, prepared, cancellationToken));
    }

    private static async IAsyncEnumerable<SseItem<ChatEvent>> Events(ChatService chat, PreparedQuestion prepared, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in chat.StreamAnswerAsync(prepared, cancellationToken))
        {
            yield return new SseItem<ChatEvent>(item, item.Type);
        }
    }
}

internal sealed record AskRequest(string Question, Guid? ContractVersionId = null);
