namespace ContractGuard.Application.Abstractions;

// Sends one plain text email (Mailpit locally, SES on AWS: SD-16).
// Throws DomainException with ERR-INTG-001 when the email could not be handed over.
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}
