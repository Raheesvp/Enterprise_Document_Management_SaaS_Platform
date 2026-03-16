using MediatR;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;

namespace WorkflowService.Application.Queries.GetWorkflowStatus;

public record GetWorkflowStatusQuery(
    Guid WorkflowInstanceId,
    Guid TenantId)
    : IRequest<Result<WorkflowInstanceDto>>;
