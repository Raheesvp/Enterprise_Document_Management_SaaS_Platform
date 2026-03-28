using Shared.Domain.Events;

namespace DocumentService.Domain.Events;

// Raised when a new version is uploaded to an existing document
// Consumed by: Parser Service (re-run OCR on new version)
//              Audit Service (log version addition)
public sealed record DocumentVersionAddedEvent(
    Guid EventId,
    Guid DocumentId,
    Guid TenantId,
    Guid UploadedByUserId,
    int VersionNumber,
    string StoragePath,
    DateTime OccuredOn,
    DateTime AddedAt) : IDomainEvent;