using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Common;

namespace ContractGuard.IntegrationTests;

// Stand-in for SMTP: keeps the emails so tests can open the links in them, as a user would.
public sealed partial class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, List<string>> _bodiesByRecipient = new();
    private readonly ConcurrentDictionary<string, byte> _failingRecipients = new();

    public void FailFor(string to) => _failingRecipients[to] = 0;

    public void StopFailingFor(string to) => _failingRecipients.TryRemove(to, out _);

    public int CountFor(string to) => _bodiesByRecipient.TryGetValue(to, out var bodies) ? bodies.Count : 0;

    // The ids carried by the newest email sent to this address.
    public (Guid UserId, string Token) LastLink(string to)
    {
        var match = LinkPattern().Match(_bodiesByRecipient[to][^1]);
        return (Guid.Parse(match.Groups["user"].Value), Uri.UnescapeDataString(match.Groups["token"].Value));
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (_failingRecipients.ContainsKey(to))
        {
            throw new DomainException(ErrorCodes.EmailSendFailed, "The email could not be sent.");
        }

        _bodiesByRecipient.GetOrAdd(to, _ => []).Add(body);
        return Task.CompletedTask;
    }

    [GeneratedRegex(@"userId=(?<user>[0-9a-f-]+)&token=(?<token>\S+)")]
    private static partial Regex LinkPattern();
}
