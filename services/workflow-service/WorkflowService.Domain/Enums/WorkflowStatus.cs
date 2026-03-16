namespace WorkflowService.Domain.Enums;

// WorkflowStatus — tracks the overall state of a workflow instance
public enum WorkflowStatus
{
    NotStarted  = 0,
    InProgress  = 1,
    Approved    = 2,
    Rejected    = 3,
    Cancelled   = 4,
    Escalated   = 5
}
