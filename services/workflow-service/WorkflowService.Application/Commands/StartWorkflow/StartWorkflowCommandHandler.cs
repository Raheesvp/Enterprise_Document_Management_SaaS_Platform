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
        var workflow = await _repository.GetByDocumentIdAsync(
            request.DocumentId,
            request.TenantId,
            cancellationToken);

        if (workflow is null)
        {
            // Create new workflow instance
            workflow = WorkflowInstance.Create(
                request.TenantId,
                request.DocumentId,
                request.WorkflowDefinitionId,
                request.DocumentTitle,
                request.InitiatedByUserId);
        }
        else
        {
            // Restart existing workflow for this document
            workflow.Reinitialize(
                request.WorkflowDefinitionId,
                request.DocumentTitle,
                request.InitiatedByUserId);
        }

        // Create stages from assignments (re-adds to cleared list if restarting)
        foreach (var assignment in request.StageAssignments
            .OrderBy(s => s.StageOrder))
        {
            var slaDeadline = DateTime.UtcNow
                .AddDays(assignment.SlaDays);

            var stage = WorkflowStage.Create(
                workflow.Id,
                assignment.StageOrder,
                assignment.StageName,
                assignment.AssignedToUserId,
                assignment.AssignedToEmail,
                slaDeadline);

            workflow.AddStage(stage);
        }

        // Start workflow
        workflow.Start();

        if (await _repository.GetByDocumentIdAsync(request.DocumentId, request.TenantId, cancellationToken) == null)
            await _repository.AddAsync(workflow, cancellationToken);
        else
            await _repository.UpdateAsync(workflow, cancellationToken);

        // Publish WorkflowStartedEvent to RabbitMQ
        // Notification Service picks this up and sends email
        var firstStage = workflow.GetCurrentStage();
        if (firstStage is not null)
        {
            await _publishEndpoint.Publish(
                new WorkflowStartedEvent
                {
                    TenantId             = workflow.TenantId,
                    WorkflowInstanceId   = workflow.Id,
                    DocumentId           = workflow.DocumentId,
                    CurrentStageName     = firstStage.StageName,
                    AssignedToUserId     = firstStage.AssignedToUserId,
                    AssignedToEmail      = firstStage.AssignedToEmail,
                    SLADeadline          = firstStage.SlaDeadline
                },
                cancellationToken);
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
