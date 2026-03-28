using MassTransit;
using Shared.Contracts.IntegrationEvents.Workflow;
using DocumentService.Application.Interfaces;
using DocumentService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace DocumentService.Infrastructure.Messaging.Consumers;

public sealed class WorkflowRejectedConsumer : IConsumer<WorkflowRejectedEvent>
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<WorkflowRejectedConsumer> _logger;

    public WorkflowRejectedConsumer(
        IDocumentRepository repository,
        ILogger<WorkflowRejectedConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WorkflowRejectedEvent> context)
    {
        _logger.LogInformation(
            "[WorkflowRejectedConsumer] Handling for document: {DocumentId}", 
            context.Message.DocumentId);

        var document = await _repository.GetByIdAsync(
            context.Message.DocumentId, 
            context.Message.TenantId, 
            context.CancellationToken);

        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} not found.", context.Message.DocumentId);
            return;
        }

        document.Reject();
        await _repository.UpdateAsync(document, context.CancellationToken);

        _logger.LogInformation("Document {DocumentId} marked as Rejected.", context.Message.DocumentId);
    }
}
