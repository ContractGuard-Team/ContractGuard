using System.Net.Mail;
using System.Text;
using ContractGuard.Application.Abstractions;
using ContractGuard.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractGuard.Infrastructure.Email;

// Adapter: implements IEmailSender over SMTP. Plain text to Mailpit locally; with a username it logs in
// over STARTTLS, which is how Gmail SMTP (smtp.gmail.com:587, App Password) works.
internal sealed partial class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var email = options.Value;
        using var client = new SmtpClient(email.SmtpHost, email.SmtpPort);
        if (!string.IsNullOrEmpty(email.SmtpUsername))
        {
            client.EnableSsl = true; // never send the password in clear text
            client.Credentials = new System.Net.NetworkCredential(email.SmtpUsername, email.SmtpPassword);
        }

        using var message = new MailMessage(email.FromAddress, to, subject, body)
        {
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            LogSendFailed(ex);
            throw new DomainException(ErrorCodes.EmailSendFailed, "The email could not be sent.");
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Sending an email failed")]
    private partial void LogSendFailed(Exception ex);
}
