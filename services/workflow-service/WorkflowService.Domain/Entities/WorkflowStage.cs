using Shared.Domain.Primitives;
using WorkflowService.Domain.Enums;

namespace WorkflowService.Domain.Entities;

public sealed class WorkflowStage : BaseEntity<Guid>
{
    public Guid WorkflowInstanceId { get; private set; }
    public int StageOrder { get; private set; }
    public string StageName { get; private set; } = string.Empty;
    public Guid AssignedToUserId { get; private set; }
    public string AssignedToEmail { get; private set; } = string.Empty;
    public StageStatus Status { get; private set; }
    public string? Comments { get; private set; }
    public DateTime SlaDeadline { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WorkflowStage(Guid id) : base(id) { }

    private WorkflowStage() { }

    public static WorkflowStage Create(
        Guid workflowInstanceId,
        int stageOrder,
        string stageName,
        Guid assignedToUserId,
        string assignedToEmail,
        DateTime slaDeadline)
    {
        return new WorkflowStage(Guid.NewGuid())
        {
            WorkflowInstanceId = workflowInstanceId,
            StageOrder         = stageOrder,
            StageName          = stageName,
            AssignedToUserId   = assignedToUserId,
            AssignedToEmail    = assignedToEmail,
            Status             = StageStatus.Pending,
            SlaDeadline        = slaDeadline,
            CreatedAt          = DateTime.UtcNow
        };
    }

    public void Start()
        => Status = StageStatus.InProgress;

    public void Approve(string? comments = null)
    {
        Status      = StageStatus.Approved;
        Comments    = comments;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject(string? comments = null)
    {
        Status      = StageStatus.Rejected;
        Comments    = comments;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Escalate()
    {
        Status      = StageStatus.Escalated;
        ProcessedAt = DateTime.UtcNow;
    }

    public bool IsOverdue()
        => Status == StageStatus.InProgress
           && DateTime.UtcNow > SlaDeadline;
}
