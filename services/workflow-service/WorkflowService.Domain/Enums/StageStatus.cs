namespace WorkflowService.Domain.Enums;

// StageStatus — tracks individual approval stage state
public enum StageStatus
{
    Pending    = 0,
    InProgress = 1,
    Approved   = 2,
    Rejected   = 3,
    Skipped    = 4,
    Escalated  = 5
}
