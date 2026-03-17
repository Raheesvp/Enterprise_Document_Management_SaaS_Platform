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
        // Check no existing workflow for this document
        var existing = await _repository.GetByDocumentIdAsync(
            request.DocumentId,
            request.TenantId,
            cancellationToken);

        if (existing is not null)
            return Result.Failure<WorkflowInstanceDto>(
                WorkflowErrors.Instance.AlreadyStarted);

        // Create workflow instance
        var instance = WorkflowInstance.Create(
            request.TenantId,
            request.DocumentId,
            request.WorkflowDefinitionId,
            request.DocumentTitle,
            request.InitiatedByUserId);

        // Create stages from assignments
        foreach (var assignment in request.StageAssignments
            .OrderBy(s => s.StageOrder))
        {
            var slaDeadline = DateTime.UtcNow
                .AddDays(assignment.SlaDays);

            var stage = WorkflowStage.Create(
                instance.Id,
                assignment.StageOrder,
                assignment.StageName,
                assignment.AssignedToUserId,
                assignment.AssignedToEmail,
                slaDeadline);

            instance.AddStage(stage);
        }

        // Start workflow
        instance.Start();

        await _repository.AddAsync(instance, cancellationToken);

        // Publish WorkflowStartedEvent to RabbitMQ
        // Notification Service picks this up and sends email
        var firstStage = instance.GetCurrentStage();
        if (firstStage is not null)
        {
            await _publishEndpoint.Publish(
                new WorkflowStartedEvent
                {
                    TenantId             = instance.TenantId,
                    WorkflowInstanceId   = instance.Id,
                    DocumentId           = instance.DocumentId,
                    CurrentStageName     = firstStage.StageName,
                    AssignedToUserId     = firstStage.AssignedToUserId,
                    AssignedToEmail      = firstStage.AssignedToEmail,
                    SLADeadline          = firstStage.SlaDeadline
                },
                cancellationToken);
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
