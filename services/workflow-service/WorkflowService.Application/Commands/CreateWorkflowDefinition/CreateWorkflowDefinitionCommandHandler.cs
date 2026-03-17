using MediatR;
using Shared.Domain.Common;
using WorkflowService.Application.Interfaces;
using WorkflowService.Domain.Entities;
using WorkflowService.Domain.Errors;

namespace WorkflowService.Application.Commands.CreateWorkflowDefinition;

public sealed class CreateWorkflowDefinitionCommandHandler
    : IRequestHandler<CreateWorkflowDefinitionCommand, Result<Guid>>
{
    private readonly IWorkflowRepository _repository;

    public CreateWorkflowDefinitionCommandHandler(
        IWorkflowRepository repository)
        => _repository = repository;

    public async Task<Result<Guid>> Handle(
        CreateWorkflowDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        // Validate at least one stage
        if (!request.Stages.Any())
            return Result.Failure<Guid>(
                WorkflowErrors.Definition.NoStages);

        // Create definition
        var definition = WorkflowDefinition.Create(
            request.TenantId,
            request.Name,
            request.Description);

        // Add stages in order
        foreach (var stage in request.Stages
            .OrderBy(s => s.Order))
        {
            definition.AddStage(
                stage.Order,
                stage.StageName,
                stage.RoleRequired,
                stage.SlaDays);
        }

        await _repository.AddDefinitionAsync(
            definition, cancellationToken);

        return Result.Success(definition.Id);
    }
}
