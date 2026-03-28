using MediatR;
using MassTransit;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;
using WorkflowService.Application.Interfaces;
using WorkflowService.Domain.Entities;
using WorkflowService.Domain.Errors;
using WorkflowService.Domain.StateMachines;
using WorkflowService.Domain.Enums;
using Shared.Contracts.IntegrationEvents.Workflow;

namespace WorkflowService.Application.Commands.ApproveStage;

public sealed class ApproveStageCommandHandler
    : IRequestHandler<ApproveStageCommand,
        Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public ApproveStageCommandHandler(
        IWorkflowRepository repository,
        IPublishEndpoint publishEndpoint)
    {
        _repository      = repository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        ApproveStageCommand request,
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
        if (currentStage is null)
            return Result.Failure<WorkflowInstanceDto>(
                WorkflowErrors.Stage.NotFound(Guid.Empty));

        if (currentStage.AssignedToUserId != request.UserId)
            return Result.Failure<WorkflowInstanceDto>(
                WorkflowErrors.Stage.NotAssignedToUser);

        var previousStageOrder = instance.CurrentStageOrder;
        
        // Use state machine for safe transition
        var stateMachine = new WorkflowStateMachine(instance);
        stateMachine.Approve(request.UserId, request.Comments);

        await _repository.UpdateAsync(instance, cancellationToken);

        // Publish events based on new state
        if (instance.Status == WorkflowStatus.Approved)
        {
            await _publishEndpoint.Publish(new WorkflowCompletedEvent
            {
                TenantId           = instance.TenantId,
                WorkflowInstanceId = instance.Id,
                DocumentId         = instance.DocumentId,
                DocumentTitle      = instance.DocumentTitle
            }, cancellationToken);
       
        }
        else if (instance.CurrentStageOrder > previousStageOrder)
        {
            var nextStage = instance.GetCurrentStage();
            if (nextStage != null)
            {
                await _publishEndpoint.Publish(new WorkflowStartedEvent
                {
                    TenantId           = instance.TenantId,
                    WorkflowInstanceId = instance.Id,
                    DocumentId         = instance.DocumentId,
                    CurrentStageName   = nextStage.StageName,
                    AssignedToUserId   = nextStage.AssignedToUserId,
                    AssignedToEmail    = nextStage.AssignedToEmail,
                    SLADeadline        = nextStage.SlaDeadline
                }, cancellationToken);
            }
        }

        return Result.Success(MapToDto(instance));
    }

    private static WorkflowInstanceDto MapToDto(
        WorkflowInstance instance) =>
        new(instance.Id,
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
                s.SlaDeadline, s.ProcessedAt)).ToList());
}
