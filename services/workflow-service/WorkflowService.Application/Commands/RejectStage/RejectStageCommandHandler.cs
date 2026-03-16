using MediatR;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;
using WorkflowService.Application.Interfaces;
using WorkflowService.Domain.Errors;
using WorkflowService.Domain.StateMachines;

namespace WorkflowService.Application.Commands.RejectStage;

public sealed class RejectStageCommandHandler
    : IRequestHandler<RejectStageCommand,
        Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowRepository _repository;

    public RejectStageCommandHandler(
        IWorkflowRepository repository)
        => _repository = repository;

    public async Task<Result<WorkflowInstanceDto>> Handle(
        RejectStageCommand request,
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

        if (instance.IsComplete())
            return Result.Failure<WorkflowInstanceDto>(
                WorkflowErrors.Instance.NotInProgress);

        var currentStage = instance.GetCurrentStage();
        if (currentStage?.AssignedToUserId != request.UserId)
            return Result.Failure<WorkflowInstanceDto>(
                WorkflowErrors.Stage.NotAssignedToUser);

        // Use state machine for safe transition
        var stateMachine = new WorkflowStateMachine(instance);
        stateMachine.Reject(request.UserId, request.Comments);

        await _repository.UpdateAsync(instance, cancellationToken);

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
                s.Id, s.StageOrder, s.StageName,
                s.AssignedToUserId, s.AssignedToEmail,
                s.Status.ToString(), s.Comments,
                s.SlaDeadline, s.ProcessedAt)).ToList()));
    }
}
