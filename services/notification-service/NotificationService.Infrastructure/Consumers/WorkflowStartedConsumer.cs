using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Commands.CreateNotification;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Services;
using Shared.Contracts.IntegrationEvents.Workflow;

namespace NotificationService.Infrastructure.Consumers;

// WorkflowStartedConsumer — listens to RabbitMQ
// When workflow starts:
// 1. Creates in-app notification in PostgreSQL
// 2. Sends email to assigned approver via MailKit
//
// Email failure never crashes the workflow
// MailKitEmailService catches all exceptions internally
public sealed class WorkflowStartedConsumer
    : IConsumer<WorkflowStartedEvent>
{
    private readonly IMediator _mediator;
    private readonly IEmailService _emailService;
    private readonly ILogger<WorkflowStartedConsumer> _logger;

    public WorkflowStartedConsumer(
        IMediator mediator,
        IEmailService emailService,
        ILogger<WorkflowStartedConsumer> logger)
    {
        _mediator     = mediator;
        _emailService = emailService;
        _logger       = logger;
    }

    public async Task Consume(
        ConsumeContext<WorkflowStartedEvent> context)
    {
        var evt = context.Message;

        _logger.LogInformation(
            "WorkflowStarted received. " +
            "WorkflowId: {WorkflowId} " +
            "Stage: {StageName}",
            evt.WorkflowInstanceId,
            evt.CurrentStageName);

        // Step 1 — Create in-app notification
        var command = new CreateNotificationCommand(
            TenantId:      evt.TenantId,
            UserId:        evt.AssignedToUserId,
            Title:         "Document Awaiting Your Approval",
            Message:       $"A document requires your approval. " +
                           $"Stage: {evt.CurrentStageName}. " +
                           $"Deadline: " +
                           $"{evt.SLADeadline:dd MMM yyyy}",
            Type:          NotificationType.StageAssigned,
            ReferenceId:   evt.WorkflowInstanceId,
            ReferenceType: "WorkflowInstance");

        var result = await _mediator.Send(command,
            context.CancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError(
                "Failed to create notification. " +
                "WorkflowId: {WorkflowId}",
                evt.WorkflowInstanceId);
        }

        // Step 2 — Send email to assigned approver
        // AssignedToEmail is from the WorkflowStartedEvent
        await _emailService.SendWorkflowAssignedAsync(
            toEmail:            evt.AssignedToEmail,
            toName:             evt.AssignedToEmail,
            documentTitle:      $"Document {evt.DocumentId}",
            stageName:          evt.CurrentStageName,
            slaDeadline:        evt.SLADeadline,
            workflowInstanceId: evt.WorkflowInstanceId,
            ct:                 context.CancellationToken);

        _logger.LogInformation(
            "Email sent to approver. " +
            "Email: {Email} WorkflowId: {WorkflowId}",
            evt.AssignedToEmail,
            evt.WorkflowInstanceId);
    }
}
