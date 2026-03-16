using MediatR;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;
using WorkflowService.Application.Interfaces;
using WorkflowService.Domain.Errors;

namespace WorkflowService.Application.Queries.GetWorkflowStatus;

public sealed class GetWorkflowStatusQueryHandler
    : IRequestHandler<GetWorkflowStatusQuery,
        Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowRepository _repository;

    public GetWorkflowStatusQueryHandler(
        IWorkflowRepository repository)
        => _repository = repository;

    public async Task<Result<WorkflowInstanceDto>> Handle(
        GetWorkflowStatusQuery request,
        CancellationToken cancellationToken)
    {
        var instance = await _repository.GetByIdAsync(
            request.WorkflowInstanceId,
            request.TenantId,
            cancellationToken);

        if (instance is null)
            return Result.Failure<WorkflowInstanceDto>(
                WorkflowErrors.Instance.NotFound(
                    request.WorkflowInstanceId));

        return Result.Success(new WorkflowInstanceDto(
            instance.Id,
            instance.TenantId,
            instance.DocumentId,
            instance.DocumentTitle,
            instance.Status.ToString(),
            instance.CurrentStageOrder,
            instance.StartedAt,
            instance.CompletedAt,
            instance.Stages.Select(s => new WorkflowStageDto(
                s.Id,
                s.StageOrder,
                s.StageName,
                s.AssignedToUserId,
                s.AssignedToEmail,
                s.Status.ToString(),
                s.Comments,
                s.SlaDeadline,
                s.ProcessedAt)).ToList()));
    }
}
