using MassTransit;
using MediatR;
using Shared.Contracts.IntegrationEvents.Workflow;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;
using WorkflowService.Application.Interfaces;
using WorkflowService.Domain.Entities;
using WorkflowService.Domain.Errors;

namespace WorkflowService.Application.Commands.StartWorkflow;

public sealed class StartWorkflowCommandHandler
    : IRequestHandler<StartWorkflowCommand,
        Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public StartWorkflowCommandHandler(
        IWorkflowRepository repository,
        IPublishEndpoint publishEndpoint)
    {
        _repository      = repository;
        _publishEndpoint = publishEndpoint;
    }

  public async Task<Result<WorkflowInstanceDto>> Handle(
    StartWorkflowCommand request,
    CancellationToken cancellationToken)
{
    // 1. Check if a workflow already exists for this document
    var workflow = await _repository.GetByDocumentIdAsync(
        request.DocumentId, 
        request.TenantId, 
        cancellationToken);

    if (workflow is null)
    {
        // Create brand new
        workflow = WorkflowInstance.Create(
            request.TenantId,
            request.DocumentId,
            request.WorkflowDefinitionId,
            request.DocumentTitle,
            request.InitiatedByUserId);
    }
    else
    {
        // IMPORTANT: Clear existing stages so we don't just keep appending stages
        // to the same workflow every time a message is retried or re-uploaded.
       
        
        // Reset the state to 'Started' or 'Pending'
        workflow.Reinitialize(
            request.WorkflowDefinitionId,
            request.DocumentTitle,
            request.InitiatedByUserId);
    }

    // 2. Add the stages (this works for both New and Reinitialized)
    foreach (var assignment in request.StageAssignments.OrderBy(s => s.StageOrder))
    {
        var slaDeadline = DateTime.UtcNow.AddDays(assignment.SlaDays);

        var stage = WorkflowStage.Create(
            workflow.Id,
            assignment.StageOrder,
            assignment.StageName,
            assignment.AssignedToUserId,
            assignment.AssignedToEmail,
            slaDeadline);

        workflow.AddStage(stage);
    }

    workflow.Start();

    // 3. Save Changes
    // We already know if it's new or existing based on our initial fetch
    if (workflow.StartedAt == DateTime.MinValue || workflow.StartedAt > DateTime.UtcNow.AddSeconds(-5)) 
    {
        // Logic check: If you want to be safe, use the same check as before
        var existing = await _repository.GetByDocumentIdAsync(request.DocumentId, request.TenantId, cancellationToken);
        if (existing == null)
            await _repository.AddAsync(workflow, cancellationToken);
        else
            await _repository.UpdateAsync(workflow, cancellationToken);
    }

    // 4. Notify (RabbitMQ)
    var firstStage = workflow.GetCurrentStage();
    if (firstStage is not null)
    {
        await _publishEndpoint.Publish(new WorkflowStartedEvent {
            TenantId = workflow.TenantId,
            WorkflowInstanceId = workflow.Id,
            DocumentId = workflow.DocumentId,
            CurrentStageName = firstStage.StageName,
            AssignedToUserId = firstStage.AssignedToUserId,
            AssignedToEmail = firstStage.AssignedToEmail,
            SLADeadline = firstStage.SlaDeadline
        }, cancellationToken);
    }

    return Result.Success(MapToDto(workflow));
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
