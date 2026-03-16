using MediatR;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;

namespace WorkflowService.Application.Commands.RejectStage;

public record RejectStageCommand(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid UserId,
    string? Comments)
    : IRequest<Result<WorkflowInstanceDto>>;
