namespace Shared.Contracts.IntegrationEvents.Workflow;

/// <summary>
/// Integration Event published when a workflow is rejected.
/// This is a positional record to support easy instantiation and immutability.
/// </summary>
public sealed record WorkflowRejectedEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid TenantId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    string DocumentTitle,
    string Reason);