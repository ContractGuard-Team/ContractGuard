using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using ContractGuard.Domain.Common;
using ContractGuard.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContractGuard.UnitTests.Infrastructure;

// docs/04 C5. A handler stand-in answers Google's token and send endpoints. No network needed.
public sealed class GmailEmailSenderTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly EmailOptions Options = new()
    {
        Provider = EmailOptions.GmailProvider,
        FromAddress = "contractguard.info@gmail.com",
        FrontendBaseUrl = "http://localhost:5173",
        GmailClientId = "client",
        GmailClientSecret = "secret",
        GmailRefreshToken = "refresh"
    };

    [Fact]
    public async Task Trades_the_refresh_token_then_sends_a_utf8_message()
    {
        var handler = new FakeGoogle();
        var sender = Create(handler);

        await sender.SendAsync("a@example.com", "Xác thực email", "Nội dung tiếng Việt", Ct);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("refresh_token=refresh", handler.Requests[0].Body);
        Assert.Equal("Bearer token-1", handler.Requests[1].Authorization);

        var raw = JsonDocument.Parse(handler.Requests[1].Body).RootElement.GetProperty("raw").GetString()!;
        var mime = Encoding.UTF8.GetString(Convert.FromBase64String(raw.Replace('-', '+').Replace('_', '/').PadRight((raw.Length + 3) / 4 * 4, '=')));
        Assert.Contains("To: a@example.com\r\n", mime);
        Assert.Contains($"Subject: =?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes("Xác thực email"))}?=", mime);
        var body = mime[(mime.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4)..];
        Assert.Equal("Nội dung tiếng Việt", Encoding.UTF8.GetString(Convert.FromBase64String(body)));
    }

    // ERR-INTG-001: same error as SMTP and SES, so registration keeps the account and offers to resend.
    [Fact]
    public async Task A_refused_send_becomes_the_send_failed_error()
    {
        var sender = Create(new FakeGoogle { SendStatus = HttpStatusCode.Forbidden });

        var error = await Assert.ThrowsAsync<DomainException>(() => sender.SendAsync("a@example.com", "s", "b", Ct));

        Assert.Equal(ErrorCodes.EmailSendFailed, error.ErrorCode);
    }

    [Fact]
    public async Task A_line_break_in_the_address_is_refused_before_calling_google()
    {
        var handler = new FakeGoogle();

        await Assert.ThrowsAsync<DomainException>(() => Create(handler).SendAsync("a@example.com\r\nBcc: x@example.com", "s", "b", Ct));

        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("refresh", true)]
    [InlineData("", false)]
    public void Gmail_needs_a_refresh_token(string refreshToken, bool valid)
    {
        var options = new EmailOptions
        {
            Provider = EmailOptions.GmailProvider,
            FromAddress = "contractguard.info@gmail.com",
            FrontendBaseUrl = "http://localhost:5173",
            GmailClientId = "client",
            GmailClientSecret = "secret",
            GmailRefreshToken = refreshToken
        };

        Assert.Equal(valid, Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true));
    }

    private static GmailEmailSender Create(FakeGoogle handler) =>
        new(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(Options), NullLogger<GmailEmailSender>.Instance);

    private sealed class FakeGoogle : HttpMessageHandler
    {
        public List<(string Body, string? Authorization)> Requests { get; } = [];
        public HttpStatusCode SendStatus { get; init; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((await request.Content!.ReadAsStringAsync(cancellationToken), request.Headers.Authorization?.ToString()));
            return request.RequestUri!.Host == "oauth2.googleapis.com"
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"token-1","expires_in":3599}""", Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(SendStatus) { Content = new StringContent("{}") };
        }
    }
}
