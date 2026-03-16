using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Commands.CreateNotification;
using NotificationService.Domain.Enums;
using Shared.Contracts.IntegrationEvents.Workflow;

namespace NotificationService.Infrastructure.Consumers;

public sealed class WorkflowStartedConsumer
    : IConsumer<WorkflowStartedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<WorkflowStartedConsumer> _logger;

    public WorkflowStartedConsumer(
        IMediator mediator,
        ILogger<WorkflowStartedConsumer> logger)
    {
        _mediator = mediator;
        _logger   = logger;
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
    }
}
