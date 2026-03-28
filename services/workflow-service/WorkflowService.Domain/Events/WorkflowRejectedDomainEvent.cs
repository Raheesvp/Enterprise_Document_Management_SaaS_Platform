using Shared.Domain.Events;

namespace WorkflowService.Domain.Events;

/// <summary>
/// Domain Event raised within the Workflow boundary when a workflow is rejected.
/// </summary>
public sealed record WorkflowRejectedDomainEvent(
    Guid EventId,
    DateTime OccuredOn,
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid DocumentId,
    string DocumentTitle,
    string Reason) : IDomainEvent;
