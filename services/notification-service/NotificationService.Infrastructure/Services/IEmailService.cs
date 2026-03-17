namespace NotificationService.Infrastructure.Services;

// IEmailService — abstraction for sending emails
// MailKitEmailService implements this for production
// StubEmailService can implement for testing
// Swap provider with zero application code change
public interface IEmailService
{
    Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default);

    Task SendWorkflowAssignedAsync(
        string toEmail,
        string toName,
        string documentTitle,
        string stageName,
        DateTime slaDeadline,
        Guid workflowInstanceId,
        CancellationToken ct = default);

    Task SendWorkflowCompletedAsync(
        string toEmail,
        string toName,
        string documentTitle,
        string finalStatus,
        CancellationToken ct = default);

    Task SendWorkflowEscalatedAsync(
        string toEmail,
        string toName,
        string documentTitle,
        string stageName,
        CancellationToken ct = default);
}
