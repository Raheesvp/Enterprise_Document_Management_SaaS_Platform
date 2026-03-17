using MediatR;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;
using WorkflowService.Application.Interfaces;

namespace WorkflowService.Application.Queries.GetWorkflowDefinitions;

public sealed class GetWorkflowDefinitionsQueryHandler
    : IRequestHandler<GetWorkflowDefinitionsQuery,
        Result<List<WorkflowDefinitionDto>>>
{
    private readonly IWorkflowRepository _repository;

    public GetWorkflowDefinitionsQueryHandler(
        IWorkflowRepository repository)
        => _repository = repository;

    public async Task<Result<List<WorkflowDefinitionDto>>> Handle(
        GetWorkflowDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _repository
            .GetDefinitionsByTenantAsync(
                request.TenantId,
                cancellationToken);

        var dtos = definitions.Select(d =>
            new WorkflowDefinitionDto(
                d.Id,
                d.TenantId,
                d.Name,
                d.Description,
                d.IsActive,
                d.CreatedAt,
                d.Stages.Select(s => new StageDefinitionDto(
                    s.Order,
                    s.StageName,
                    s.RoleRequired,
                    s.SlaDays)).ToList()))
            .ToList();

        return Result.Success(dtos);
    }
}
