using MediatR;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;

namespace WorkflowService.Application.Commands.ApproveStage;

public record ApproveStageCommand(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid UserId,
    string? Comments)
    : IRequest<Result<WorkflowInstanceDto>>;
