using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts.IntegrationEvents.Documents;
using WorkflowService.Application.Commands.StartWorkflow;

namespace WorkflowService.Infrastructure.Consumers;

// DocumentUploadedConsumer — listens for document uploads
// When a document is uploaded this consumer fires automatically
// It starts a default workflow for the document
//
// Flow:
// Document uploaded ? DocumentUploadEvent published
// This consumer picks it up from RabbitMQ
// Creates a workflow instance with default stages
// WorkflowStartedEvent published back to RabbitMQ
// Notification Service picks up WorkflowStartedEvent
// Approver gets notification
public sealed class DocumentUploadedConsumer
    : IConsumer<DocumentUploadEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentUploadedConsumer> _logger;

    public DocumentUploadedConsumer(
        IMediator mediator,
        ILogger<DocumentUploadedConsumer> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    public async Task Consume(
        ConsumeContext<DocumentUploadEvent> context)
    {
        var evt = context.Message;

        _logger.LogInformation(
            "DocumentUploaded received. " +
            "DocumentId: {DocumentId} " +
            "TenantId: {TenantId}",
            evt.DocumentId,
            evt.TenantId);

        // Start default single-stage workflow
        // In production this would look up the tenant workflow template
        var command = new StartWorkflowCommand(
            TenantId:             evt.TenantId,
            DocumentId:           evt.DocumentId,
            DocumentTitle:        evt.FileName,
            InitiatedByUserId:    evt.UploadedByUserId,
            WorkflowDefinitionId: Guid.NewGuid(),
            StageAssignments: new List<StageAssignment>
            {
                new StageAssignment(
                    StageOrder:      1,
                    StageName:       "Document Review",
                    AssignedToUserId: evt.UploadedByUserId,
                    AssignedToEmail:  "reviewer@company.com",
                    SlaDays:          3)
            });

        var result = await _mediator.Send(command,
            context.CancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to start workflow. " +
                "DocumentId: {DocumentId} " +
                "Reason: {Reason}",
                evt.DocumentId,
                result.Error.Description);
        }
        else
        {
            _logger.LogInformation(
                "Workflow started. " +
                "DocumentId: {DocumentId} " +
                "WorkflowId: {WorkflowId}",
                evt.DocumentId,
                result.Value.Id);
        }
    }
}
