using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.Email;

// Adapter: implements IEmailSender with the Gmail API over HTTPS (docs/04 C5). Railway blocks outbound SMTP
// below the Pro plan, and SES needs a domain to leave the sandbox. Sends as the Gmail account that granted the
// refresh token (scope gmail.send), so the mail passes Gmail's own SPF/DKIM.
internal sealed partial class GmailEmailSender(HttpClient http, IOptions<EmailOptions> options, ILogger<GmailEmailSender> logger) : IEmailSender
{
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string SendUrl = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        // The address goes into a raw header: a line break would let it add headers of its own.
        if (to.AsSpan().IndexOfAny('\r', '\n') >= 0)
        {
            throw new DomainException(ErrorCodes.EmailSendFailed, "The email could not be sent.");
        }

        var email = options.Value;
        try
        {
            // shortcut: a new access token per email; cache it for its hour if sending ever gets busy.
            using var tokenResponse = await http.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = email.GmailClientId!,
                ["client_secret"] = email.GmailClientSecret!,
                ["refresh_token"] = email.GmailRefreshToken!,
                ["grant_type"] = "refresh_token"
            }), cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();
            var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, SendUrl)
            {
                Content = JsonContent.Create(new { raw = Base64Url(Mime(email.FromAddress, to, subject, body)) })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
            using var sendResponse = await http.SendAsync(request, cancellationToken);
            sendResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // Only the status: Google's error bodies can name the recipient, and emails are not logged.
            LogSendFailed((ex as HttpRequestException)?.StatusCode?.ToString() ?? ex.GetType().Name);
            throw new DomainException(ErrorCodes.EmailSendFailed, "The email could not be sent.");
        }
    }

    // Plain text in UTF-8; the subject as an RFC 2047 encoded word so Vietnamese survives the header.
    internal static string Mime(string from, string to, string subject, string body) =>
        $"From: {from}\r\nTo: {to}\r\nSubject: =?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(subject))}?=\r\n" +
        "MIME-Version: 1.0\r\nContent-Type: text/plain; charset=UTF-8\r\nContent-Transfer-Encoding: base64\r\n\r\n" +
        Convert.ToBase64String(Encoding.UTF8.GetBytes(body), Base64FormattingOptions.InsertLineBreaks);

    private static string Base64Url(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sending an email through the Gmail API failed: {Code}")]
    private partial void LogSendFailed(string code);
}
