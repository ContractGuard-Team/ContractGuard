using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.Email;

// Adapter: implements IEmailSender with Amazon SES (SD-16). Credentials come from the SDK chain:
// an AWS profile on a developer machine, the task role on Fargate. No SMTP password to keep.
internal sealed partial class SesEmailSender(
    IAmazonSimpleEmailServiceV2 ses,
    IOptions<EmailOptions> options,
    ILogger<SesEmailSender> logger) : IEmailSender
{
    private const string Utf8 = "UTF-8";

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = options.Value.FromAddress,
            Destination = new Destination { ToAddresses = [to] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject, Charset = Utf8 },
                    Body = new Body { Text = new Content { Data = body, Charset = Utf8 } }
                }
            }
        };

        try
        {
            await ses.SendEmailAsync(request, cancellationToken);
        }
        catch (AmazonServiceException ex)
        {
            // Only the code: SES messages name the rejected address, and emails are not logged.
            LogSendFailed(ex.ErrorCode ?? ex.GetType().Name);
            throw new DomainException(ErrorCodes.EmailSendFailed, "The email could not be sent.");
        }
        catch (AmazonClientException ex)
        {
            // No credentials, no network: nothing about the recipient in it.
            LogSendFailed(ex.GetType().Name);
            throw new DomainException(ErrorCodes.EmailSendFailed, "The email could not be sent.");
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Sending an email through SES failed: {Code}")]
    private partial void LogSendFailed(string code);
}
