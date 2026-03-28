using MassTransit;
using MediatR;
using Shared.Contracts.IntegrationEvents.Workflow;
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
    private readonly IPublishEndpoint _publishEndpoint;

    public RejectStageCommandHandler(
        IWorkflowRepository repository,
        IPublishEndpoint publishEndpoint)
    {
        _repository      = repository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        RejectStageCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Fetch the entity (using 'instance' as the name)
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

        // 2. Transition State
        var stateMachine = new WorkflowStateMachine(instance);
        stateMachine.Reject(request.UserId, request.Comments);

        await _repository.UpdateAsync(instance, cancellationToken);

        // 3. Publish Event (FIXED: Changed 'workflow' to 'instance')
        await _publishEndpoint.Publish(new WorkflowRejectedEvent(
            Guid.NewGuid(),           // EventId
            DateTime.UtcNow,          // OccurredOn
            instance.TenantId,        // Changed from workflow to instance
            instance.Id,              // Changed from workflow to instance
            instance.DocumentId,      // Changed from workflow to instance
            instance.DocumentTitle,   // Changed from workflow to instance
            request.Comments ?? "No reason provided"
        ), cancellationToken);

        // 4. Return the DTO
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
