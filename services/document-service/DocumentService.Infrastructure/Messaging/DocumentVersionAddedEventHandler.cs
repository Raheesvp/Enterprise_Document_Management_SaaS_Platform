using DocumentService.Domain.Events;
using DocumentService.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts.IntegrationEvents.Documents;

namespace DocumentService.Infrastructure.Messaging;

public sealed class DocumentVersionAddedEventHandler
    : INotificationHandler<DocumentVersionAddedEvent>
{
    private readonly IDocumentRepository _documentRepo;
    private readonly IDocumentEventPublisher _publisher;
    private readonly ILogger<DocumentVersionAddedEventHandler> _logger;

    public DocumentVersionAddedEventHandler(
        IDocumentRepository documentRepo,
        IDocumentEventPublisher publisher,
        ILogger<DocumentVersionAddedEventHandler> logger)
    {
        _documentRepo = documentRepo;
        _publisher    = publisher;
        _logger       = logger;
    }

    public async Task Handle(
        DocumentVersionAddedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DOMAIN EVENT] DocumentVersionAdded. Handling... " +
            "DocumentId: {DocumentId} Version: {VersionNumber}",
            domainEvent.DocumentId,
            domainEvent.VersionNumber);

        // Fetch latest document metadata
        var document = await _documentRepo.GetByIdAsync(
            domainEvent.DocumentId, domainEvent.TenantId, cancellationToken);
        
        if (document is null)
        {
            _logger.LogError(
                "Document {DocumentId} not found when handling version added event.",
                domainEvent.DocumentId);
            return;
        }

        var integrationEvent = new DocumentUploadEvent
        {
            EventId          = domainEvent.EventId,
            OccuredOn        = domainEvent.OccuredOn,
            TenantId         = domainEvent.TenantId,
            DocumentId       = domainEvent.DocumentId,
            UploadedByUserId = domainEvent.UploadedByUserId,
            FileName         = document.Title.Value,
            ContentType      = document.ContentType.MimeType,
            StoragePath      = domainEvent.StoragePath
        };

        // Reuse the same publisher to send DocumentUploadEvent
        // WorkflowService will receive this and (re)start the workflow
        await _publisher.PublishDocumentUploadedAsync(
            integrationEvent,
            cancellationToken);

        _logger.LogInformation(
            "[INTEGRATION EVENT] DocumentUploadEvent published for new version. " +
            "DocumentId: {DocumentId} Version: {VersionNumber}",
            domainEvent.DocumentId,
            domainEvent.VersionNumber);
    }
}
