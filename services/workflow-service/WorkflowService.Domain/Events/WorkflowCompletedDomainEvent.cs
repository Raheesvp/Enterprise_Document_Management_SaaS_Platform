using Shared.Domain.Events;

namespace WorkflowService.Domain.Events;

public record WorkflowCompletedDomainEvent(
    Guid EventId,
    DateTime OccuredOn,
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid DocumentId,
    string DocumentTitle) : IDomainEvent;
