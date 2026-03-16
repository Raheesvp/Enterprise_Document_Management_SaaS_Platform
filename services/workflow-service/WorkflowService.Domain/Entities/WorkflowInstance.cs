using Shared.Domain.Primitives;
using WorkflowService.Domain.Enums;

namespace WorkflowService.Domain.Entities;

public sealed class WorkflowInstance : AggregateRoot<Guid>
{
    private readonly List<WorkflowStage> _stages = new();

    public Guid TenantId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid WorkflowDefinitionId { get; private set; }
    public string DocumentTitle { get; private set; } = string.Empty;
    public WorkflowStatus Status { get; private set; }
    public int CurrentStageOrder { get; private set; }
    public Guid InitiatedByUserId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public IReadOnlyList<WorkflowStage> Stages
        => _stages.AsReadOnly();

    private WorkflowInstance(Guid id) : base(id) { }

    private WorkflowInstance() { }

    public static WorkflowInstance Create(
        Guid tenantId,
        Guid documentId,
        Guid workflowDefinitionId,
        string documentTitle,
        Guid initiatedByUserId)
    {
        return new WorkflowInstance(Guid.NewGuid())
        {
            TenantId             = tenantId,
            DocumentId           = documentId,
            WorkflowDefinitionId = workflowDefinitionId,
            DocumentTitle        = documentTitle,
            Status               = WorkflowStatus.NotStarted,
            CurrentStageOrder    = 0,
            InitiatedByUserId    = initiatedByUserId,
            StartedAt            = DateTime.UtcNow
        };
    }

    public void AddStage(WorkflowStage stage)
        => _stages.Add(stage);

    public void Start()
    {
        Status            = WorkflowStatus.InProgress;
        CurrentStageOrder = 1;

        var firstStage = _stages
            .FirstOrDefault(s => s.StageOrder == 1);
        firstStage?.Start();
    }

    public bool ApproveCurrentStage(
        Guid userId,
        string? comments = null)
    {
        var currentStage = GetCurrentStage();
        if (currentStage is null) return false;

        currentStage.Approve(comments);

        var nextStage = _stages
            .FirstOrDefault(
                s => s.StageOrder == CurrentStageOrder + 1);

        if (nextStage is null)
        {
            Status      = WorkflowStatus.Approved;
            CompletedAt = DateTime.UtcNow;
            return true;
        }

        CurrentStageOrder++;
        nextStage.Start();
        return false;
    }

    public void RejectCurrentStage(
        Guid userId,
        string? comments = null)
    {
        var currentStage = GetCurrentStage();
        currentStage?.Reject(comments);

        Status      = WorkflowStatus.Rejected;
        CompletedAt = DateTime.UtcNow;
    }

    public void EscalateCurrentStage()
    {
        var currentStage = GetCurrentStage();
        currentStage?.Escalate();
        Status = WorkflowStatus.Escalated;
    }

    public void Cancel()
    {
        Status      = WorkflowStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    public WorkflowStage? GetCurrentStage()
        => _stages.FirstOrDefault(
            s => s.StageOrder == CurrentStageOrder);

    public bool IsComplete()
        => Status is WorkflowStatus.Approved
               or WorkflowStatus.Rejected
               or WorkflowStatus.Cancelled;
}
