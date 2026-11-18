using System.ComponentModel.DataAnnotations;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using ContractGuard.Domain.Common;
using ContractGuard.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContractGuard.UnitTests.Infrastructure;

// SD-16. A client stand-in records the request instead of calling SES. No network needed.
public sealed class SesEmailSenderTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly EmailOptions Options = new()
    {
        Provider = EmailOptions.SesProvider,
        FromAddress = "no-reply@example.com",
        FrontendBaseUrl = "http://localhost:5173"
    };

    [Fact]
    public async Task Sends_plain_text_in_utf8_from_the_configured_address()
    {
        using var ses = new RecordingSes();
        var sender = new SesEmailSender(ses, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<SesEmailSender>.Instance);

        await sender.SendAsync("a@example.com", "Xác thực email", "Nội dung tiếng Việt", Ct);

        var request = Assert.Single(ses.Requests);
        Assert.Equal("no-reply@example.com", request.FromEmailAddress);
        Assert.Equal(["a@example.com"], request.Destination.ToAddresses);
        Assert.Equal("Xác thực email", request.Content.Simple.Subject.Data);
        Assert.Equal("UTF-8", request.Content.Simple.Subject.Charset);
        Assert.Equal("Nội dung tiếng Việt", request.Content.Simple.Body.Text.Data);
        Assert.Equal("UTF-8", request.Content.Simple.Body.Text.Charset);
    }

    // ERR-INTG-001: same error as SMTP, so registration keeps the account and offers to resend.
    [Fact]
    public async Task A_rejected_email_becomes_the_send_failed_error()
    {
        using var ses = new RecordingSes { Failure = new MessageRejectedException("Email address is not verified: a@example.com") };
        var sender = new SesEmailSender(ses, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<SesEmailSender>.Instance);

        var error = await Assert.ThrowsAsync<DomainException>(() => sender.SendAsync("a@example.com", "s", "b", Ct));

        Assert.Equal(ErrorCodes.EmailSendFailed, error.ErrorCode);
    }

    [Fact]
    public async Task Missing_credentials_become_the_send_failed_error()
    {
        using var ses = new RecordingSes { Failure = new AmazonClientException("Failed to resolve AWS credentials") };
        var sender = new SesEmailSender(ses, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<SesEmailSender>.Instance);

        var error = await Assert.ThrowsAsync<DomainException>(() => sender.SendAsync("a@example.com", "s", "b", Ct));

        Assert.Equal(ErrorCodes.EmailSendFailed, error.ErrorCode);
    }

    [Theory]
    [InlineData(EmailOptions.SmtpProvider, "", false)]
    [InlineData(EmailOptions.SmtpProvider, "localhost", true)]
    [InlineData(EmailOptions.SesProvider, "", true)]
    [InlineData("sendgrid", "localhost", false)]
    public void Smtp_needs_a_host_and_ses_does_not(string provider, string smtpHost, bool valid)
    {
        var options = new EmailOptions
        {
            Provider = provider,
            SmtpHost = smtpHost,
            FromAddress = "no-reply@example.com",
            FrontendBaseUrl = "http://localhost:5173"
        };

        Assert.Equal(valid, Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true));
    }

    private sealed class RecordingSes() : AmazonSimpleEmailServiceV2Client(new AnonymousAWSCredentials(), RegionEndpoint.APSoutheast1)
    {
        public List<SendEmailRequest> Requests { get; } = [];
        public Exception? Failure { get; init; }

        public override Task<SendEmailResponse> SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Failure is null ? Task.FromResult(new SendEmailResponse()) : Task.FromException<SendEmailResponse>(Failure);
        }
    }
}
