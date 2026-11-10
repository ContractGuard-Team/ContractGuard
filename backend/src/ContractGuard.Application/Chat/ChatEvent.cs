using ContractGuard.Domain.Chat;

namespace ContractGuard.Application.Chat;

// One server-sent event of an answer (SD-15). Type is the SSE event name: question, token, citation,
// done or error. Once the stream has started an HTTP status can no longer change, so failures arrive
// as an "error" event with the ERR code.
public sealed record ChatEvent(string Type, string? Text = null, ChatCitation? Citation = null, Guid? MessageId = null, string? Code = null);
