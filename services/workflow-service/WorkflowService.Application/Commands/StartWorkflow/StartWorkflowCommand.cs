using MediatR;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;

namespace WorkflowService.Application.Commands.StartWorkflow;

public record StartWorkflowCommand(
    Guid TenantId,
    Guid DocumentId,
    string DocumentTitle,
    Guid InitiatedByUserId,
    Guid WorkflowDefinitionId,
    List<StageAssignment> StageAssignments)
    : IRequest<Result<WorkflowInstanceDto>>;

public record StageAssignment(
    int StageOrder,
    string StageName,
    Guid AssignedToUserId,
    string AssignedToEmail,
    int SlaDays);
