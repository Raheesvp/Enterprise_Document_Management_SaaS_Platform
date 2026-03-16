namespace WorkflowService.Application.DTOs;

public record WorkflowInstanceDto(
    Guid Id,
    Guid TenantId,
    Guid DocumentId,
    string DocumentTitle,
    string Status,
    int CurrentStageOrder,
    DateTime StartedAt,
    DateTime? CompletedAt,
    List<WorkflowStageDto> Stages);

public record WorkflowStageDto(
    Guid Id,
    int StageOrder,
    string StageName,
    Guid AssignedToUserId,
    string AssignedToEmail,
    string Status,
    string? Comments,
    DateTime SlaDeadline,
    DateTime? ProcessedAt);
