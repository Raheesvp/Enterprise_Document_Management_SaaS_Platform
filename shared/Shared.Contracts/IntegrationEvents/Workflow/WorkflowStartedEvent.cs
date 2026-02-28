using System.Diagnostics.Contracts;

namespace Shared.Contracts.IntegrationEvents.Workflow;

public record WorkflowStartedEvent
{
    public Guid EventId {get;init;} = Guid.NewGuid();

    public DateTime OccurredOn {get;init;} =DateTime.UtcNow;

    public Guid TenantId {get;init;}

    public Guid WorkflowInstanceId {get;init;}

    public Guid DocumentId {get;init;}

    public string CurrentStageName {get;init;} =string.Empty; 
    public Guid AssignedToUserId {get;init;}

    public string AssignedToEmail {get;init;} = string.Empty;

    public DateTime SLADeadline {get;init;} 
}