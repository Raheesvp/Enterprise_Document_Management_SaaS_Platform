using Shared.Domain.Events;

namespace WorkflowService.Domain.Events;

public record WorkflowRejectedDomainEvent(
    Guid EventId,
    DateTime OccuredOn,
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid DocumentId,
    string DocumentTitle,
    string Reason) : IDomainEvent;
