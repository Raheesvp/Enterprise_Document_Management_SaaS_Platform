using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts.IntegrationEvents.Documents;
using WorkflowService.Application.Commands.StartWorkflow;
using WorkflowService.Application.Interfaces;

namespace WorkflowService.Infrastructure.Consumers;

// DocumentUploadedConsumer â€” listens for document uploads
//
// Flow:
// Document uploaded ? DocumentUploadEvent published
// This consumer picks it up from RabbitMQ
//
// It now looks up the tenant's active WorkflowDefinition
// and uses that template to create the workflow stages.
//
// If no template exists for the tenant it falls back
// to a default single-stage review workflow so the
// pipeline never breaks.
public sealed class DocumentUploadedConsumer
    : IConsumer<DocumentUploadEvent>
{
    private readonly IMediator _mediator;
    private readonly IWorkflowRepository _repository;
    private readonly ILogger<DocumentUploadedConsumer> _logger;

    public DocumentUploadedConsumer(
        IMediator mediator,
        IWorkflowRepository repository,
        ILogger<DocumentUploadedConsumer> logger)
    {
        _mediator   = mediator;
        _repository = repository;
        _logger     = logger;
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

        // Look up tenant's active workflow templates
        var allDefinitions = await _repository
            .GetDefinitionsByTenantAsync(
                evt.TenantId,
                context.CancellationToken);

        // If no tenant-specific definitions, look for global ones (seeded with Guid.Empty)
        if (!allDefinitions.Any())
        {
            allDefinitions = await _repository
                .GetDefinitionsByTenantAsync(
                    Guid.Empty,
                    context.CancellationToken);
        }

        // Mapping Logic: Attempt to find a workflow that matches the document type
        var documentType = GetDocumentTypeFromMimeType(evt.ContentType);
        
        var definition = allDefinitions.FirstOrDefault(d => 
            d.Name.Contains(documentType, StringComparison.OrdinalIgnoreCase))
            ?? allDefinitions.FirstOrDefault();

        List<StageAssignment> stages;
        Guid definitionId;

        if (definition is not null && definition.Stages.Any())
        {
            // Use tenant's configured template
            _logger.LogInformation(
                "Using tenant workflow template. " +
                "DefinitionId: {DefinitionId} " +
                "Name: {Name} Stages: {StageCount} " +
                "Matched Type: {MatchedType}",
                definition.Id,
                definition.Name,
                definition.Stages.Count,
                documentType);

            definitionId = definition.Id;

            stages = definition.Stages
                .OrderBy(s => s.Order)
                .Select(s => new StageAssignment(
                    StageOrder:       s.Order,
                    StageName:        s.StageName,
                    AssignedToUserId: evt.UploadedByUserId,
                    AssignedToEmail:  "reviewer@company.com",
                    SlaDays:          s.SlaDays))
                .ToList();
        }
        else
        {
            // Fall back to default single-stage workflow
            _logger.LogWarning(
                "No workflow template found for tenant. " +
                "TenantId: {TenantId} â€” using default",
                evt.TenantId);

            definitionId = Guid.NewGuid();

            stages = new List<StageAssignment>
            {
                new StageAssignment(
                    StageOrder:       1,
                    StageName:        "Document Review",
                    AssignedToUserId: evt.UploadedByUserId,
                    AssignedToEmail:  "reviewer@company.com",
                    SlaDays:          3)
            };
        }

        var command = new StartWorkflowCommand(
            TenantId:             evt.TenantId,
            DocumentId:           evt.DocumentId,
            DocumentTitle:        evt.FileName,
            InitiatedByUserId:    evt.UploadedByUserId,
            WorkflowDefinitionId: definitionId,
            StageAssignments:     stages);

        var result = await _mediator.Send(
            command,
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
                "Workflow started from template. " +
                "DocumentId: {DocumentId} " +
                "WorkflowId: {WorkflowId} " +
                "Stages: {StageCount}",
                evt.DocumentId,
                result.Value.Id,
                result.Value.Stages.Count);
        }
    }

    private static string GetDocumentTypeFromMimeType(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType)) return "General";

        return mimeType.ToLowerInvariant() switch
        {
            "application/pdf" => "PDF",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "Word",
            "application/vnd.ms-excel" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "Spreadsheet",
            "image/jpeg" or "image/png" => "Image",
            _ => "General"
        };
    }
}
