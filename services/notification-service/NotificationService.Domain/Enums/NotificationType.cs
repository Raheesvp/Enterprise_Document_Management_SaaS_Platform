namespace NotificationService.Domain.Enums;

public enum NotificationType
{
    WorkflowStarted     = 1,
    StageAssigned       = 2,
    StageApproved       = 3,
    StageRejected       = 4,
    WorkflowCompleted   = 5,
    WorkflowEscalated   = 6,
    DocumentUploaded    = 7,
    DocumentParsed      = 8
}
