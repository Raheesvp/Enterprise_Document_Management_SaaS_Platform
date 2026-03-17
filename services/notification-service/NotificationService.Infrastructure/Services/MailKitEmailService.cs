using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace NotificationService.Infrastructure.Services;

// MailKitEmailService — sends real emails via SMTP
//
// Configuration in appsettings.json:
// EmailSettings:SmtpHost     ? smtp.gmail.com
// EmailSettings:SmtpPort     ? 587
// EmailSettings:SmtpUser     ? your@gmail.com
// EmailSettings:SmtpPassword ? app password
// EmailSettings:FromEmail    ? noreply@yourdomain.com
// EmailSettings:FromName     ? Document SaaS
//
// For development — uses MailHog (local SMTP catcher)
// For production  — uses real SMTP provider
// Zero code change needed — just update appsettings
public sealed class MailKitEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(
        IConfiguration configuration,
        ILogger<MailKitEmailService> logger)
    {
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();

            // From
            message.From.Add(new MailboxAddress(
                _configuration["EmailSettings:FromName"]
                    ?? "Document SaaS",
                _configuration["EmailSettings:FromEmail"]
                    ?? "noreply@documentsaas.com"));

            // To
            message.To.Add(
                new MailboxAddress(toName, toEmail));

            message.Subject = subject;

            // Build HTML body
            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = builder.ToMessageBody();

            // Send via SMTP
            using var client = new SmtpClient();

            var host     = _configuration["EmailSettings:SmtpHost"]
                           ?? "localhost";
            var port     = int.Parse(
                           _configuration["EmailSettings:SmtpPort"]
                           ?? "1025");
            var useSSL   = bool.Parse(
                           _configuration["EmailSettings:UseSSL"]
                           ?? "false");

            await client.ConnectAsync(
                host,
                port,
                useSSL
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.None,
                ct);

            // Only authenticate if credentials provided
            var smtpUser = _configuration["EmailSettings:SmtpUser"];
            if (!string.IsNullOrEmpty(smtpUser))
            {
                await client.AuthenticateAsync(
                    smtpUser,
                    _configuration["EmailSettings:SmtpPassword"],
                    ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation(
                "Email sent. To: {Email} Subject: {Subject}",
                toEmail, subject);
        }
        catch (Exception ex)
        {
            // Log but do not throw
            // Email failure should never crash the workflow
            _logger.LogError(ex,
                "Failed to send email. To: {Email} " +
                "Subject: {Subject}",
                toEmail, subject);
        }
    }

    public async Task SendWorkflowAssignedAsync(
        string toEmail,
        string toName,
        string documentTitle,
        string stageName,
        DateTime slaDeadline,
        Guid workflowInstanceId,
        CancellationToken ct = default)
    {
        var subject  = $"Action Required: {documentTitle}";
        var htmlBody = BuildWorkflowAssignedEmail(
            toName,
            documentTitle,
            stageName,
            slaDeadline,
            workflowInstanceId);

        await SendAsync(toEmail, toName, subject, htmlBody, ct);
    }

    public async Task SendWorkflowCompletedAsync(
        string toEmail,
        string toName,
        string documentTitle,
        string finalStatus,
        CancellationToken ct = default)
    {
        var subject  = $"Document {finalStatus}: {documentTitle}";
        var htmlBody = BuildWorkflowCompletedEmail(
            toName, documentTitle, finalStatus);

        await SendAsync(toEmail, toName, subject, htmlBody, ct);
    }

    public async Task SendWorkflowEscalatedAsync(
        string toEmail,
        string toName,
        string documentTitle,
        string stageName,
        CancellationToken ct = default)
    {
        var subject  = $"ESCALATED: {documentTitle} — SLA Breached";
        var htmlBody = BuildEscalatedEmail(
            toName, documentTitle, stageName);

        await SendAsync(toEmail, toName, subject, htmlBody, ct);
    }

    // HTML email templates
    private static string BuildWorkflowAssignedEmail(
        string toName,
        string documentTitle,
        string stageName,
        DateTime slaDeadline,
        Guid workflowInstanceId)
    {
        return $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; max-width: 600px;'>
    <div style='background: #2563EB; padding: 20px;'>
        <h1 style='color: white; margin: 0;'>Document SaaS</h1>
    </div>
    <div style='padding: 30px;'>
        <h2>Action Required</h2>
        <p>Hello {toName},</p>
        <p>A document requires your approval.</p>
        <table style='width: 100%; border-collapse: collapse;'>
            <tr>
                <td style='padding: 8px; font-weight: bold;'>
                    Document:
                </td>
                <td style='padding: 8px;'>{documentTitle}</td>
            </tr>
            <tr style='background: #F3F4F6;'>
                <td style='padding: 8px; font-weight: bold;'>
                    Stage:
                </td>
                <td style='padding: 8px;'>{stageName}</td>
            </tr>
            <tr>
                <td style='padding: 8px; font-weight: bold;'>
                    Deadline:
                </td>
                <td style='padding: 8px; color: #DC2626;'>
                    {slaDeadline:dd MMM yyyy HH:mm} UTC
                </td>
            </tr>
        </table>
        <br/>
        <a href='http://localhost:3000/workflow/{workflowInstanceId}'
           style='background: #2563EB; color: white;
                  padding: 12px 24px; text-decoration: none;
                  border-radius: 4px;'>
            Review Document
        </a>
        <br/><br/>
        <p style='color: #6B7280; font-size: 12px;'>
            This is an automated notification from Document SaaS.
        </p>
    </div>
</body>
</html>";
    }

    private static string BuildWorkflowCompletedEmail(
        string toName,
        string documentTitle,
        string finalStatus)
    {
        var color = finalStatus == "Approved"
            ? "#16A34A" : "#DC2626";

        return $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; max-width: 600px;'>
    <div style='background: #2563EB; padding: 20px;'>
        <h1 style='color: white; margin: 0;'>Document SaaS</h1>
    </div>
    <div style='padding: 30px;'>
        <h2 style='color: {color};'>
            Document {finalStatus}
        </h2>
        <p>Hello {toName},</p>
        <p>
            The document <strong>{documentTitle}</strong>
            has been <strong>{finalStatus}</strong>.
        </p>
        <p style='color: #6B7280; font-size: 12px;'>
            This is an automated notification from Document SaaS.
        </p>
    </div>
</body>
</html>";
    }

    private static string BuildEscalatedEmail(
        string toName,
        string documentTitle,
        string stageName)
    {
        return $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; max-width: 600px;'>
    <div style='background: #DC2626; padding: 20px;'>
        <h1 style='color: white; margin: 0;'>
            ? SLA Breach — Document SaaS
        </h1>
    </div>
    <div style='padding: 30px;'>
        <h2 style='color: #DC2626;'>Escalation Notice</h2>
        <p>Hello {toName},</p>
        <p>
            A document approval has been escalated because
            the SLA deadline was breached.
        </p>
        <table style='width: 100%; border-collapse: collapse;'>
            <tr>
                <td style='padding: 8px; font-weight: bold;'>
                    Document:
                </td>
                <td style='padding: 8px;'>{documentTitle}</td>
            </tr>
            <tr style='background: #FEF2F2;'>
                <td style='padding: 8px; font-weight: bold;'>
                    Stage:
                </td>
                <td style='padding: 8px;'>{stageName}</td>
            </tr>
        </table>
        <p style='color: #6B7280; font-size: 12px;'>
            This is an automated notification from Document SaaS.
        </p>
    </div>
</body>
</html>";
    }
}
