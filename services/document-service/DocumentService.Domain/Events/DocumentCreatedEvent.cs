using Shared.Domain.Events;

namespace DocumentService.Domain.Events;

// Raised inside Document aggregate when first created
// Dispatched AFTER EF Core SaveChangesAsync commits
// Consumed by: Workflow Service (start approval chain)
//              Audit Service (log creation)
public sealed record DocumentCreatedEvent(
    Guid EventId,
    Guid DocumentId,
    Guid TenantId,
    Guid UploadedByUserId,
    string Title,
    string MimeType,
    string StoragePath,
    DateTime OccuredOn,
    DateTime CreatedAt) : IDomainEvent;