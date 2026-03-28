using MassTransit;
using Shared.Contracts.IntegrationEvents.Workflow;
using DocumentService.Application.Interfaces;
using DocumentService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace DocumentService.Infrastructure.Messaging.Consumers;

public sealed class WorkflowCompletedConsumer : IConsumer<WorkflowCompletedEvent>
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<WorkflowCompletedConsumer> _logger;

    public WorkflowCompletedConsumer(
        IDocumentRepository repository,
        ILogger<WorkflowCompletedConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WorkflowCompletedEvent> context)
    {
        _logger.LogInformation(
            "[WorkflowCompletedConsumer] Handling for document: {DocumentId}", 
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

        document.Approve();
        await _repository.UpdateAsync(document, context.CancellationToken);

        _logger.LogInformation("Document {DocumentId} marked as Approved.", context.Message.DocumentId);
    }
}
