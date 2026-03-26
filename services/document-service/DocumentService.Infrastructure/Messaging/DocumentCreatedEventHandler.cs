using DocumentService.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts.IntegrationEvents.Documents;

namespace DocumentService.Infrastructure.Messaging;

public sealed class DocumentCreatedEventHandler
    : INotificationHandler<DocumentCreatedEvent>
{
    private readonly IDocumentEventPublisher _publisher;
    private readonly ILogger<DocumentCreatedEventHandler> _logger;

    public DocumentCreatedEventHandler(
        IDocumentEventPublisher publisher,
        ILogger<DocumentCreatedEventHandler> logger)
    {
        _publisher = publisher;
        _logger    = logger;
    }

    public async Task Handle(
        DocumentCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DOMAIN EVENT] DocumentCreated. Handling... " +
            "DocumentId: {DocumentId} MimeType: {MimeType}",
            domainEvent.DocumentId,
            domainEvent.MimeType);

        var integrationEvent = new DocumentUploadEvent
        {
            EventId          = domainEvent.EventId,
            OccuredOn        = domainEvent.OccuredOn,
            TenantId         = domainEvent.TenantId,
            DocumentId       = domainEvent.DocumentId,
            UploadedByUserId = domainEvent.UploadedByUserId,
            FileName         = domainEvent.Title,
            ContentType      = domainEvent.MimeType,
            StoragePath      = domainEvent.StoragePath
        };

        await _publisher.PublishDocumentUploadedAsync(
            integrationEvent,
            cancellationToken);

        _logger.LogInformation(
            "[INTEGRATION EVENT] DocumentUploadEvent published. " +
            "DocumentId: {DocumentId}",
            domainEvent.DocumentId);
    }
}
