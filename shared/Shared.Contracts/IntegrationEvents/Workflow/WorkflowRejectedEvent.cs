namespace Shared.Contracts.IntegrationEvents.Workflow;

public record WorkflowRejectedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public Guid TenantId { get; init; }
    public Guid WorkflowInstanceId { get; init; }
    public Guid DocumentId { get; init; }
    public string DocumentTitle { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
