using MediatR;
using Shared.Domain.Common;
using WorkflowService.Application.DTOs;

namespace WorkflowService.Application.Queries.GetWorkflowDefinitions;

public record GetWorkflowDefinitionsQuery(
    Guid TenantId)
    : IRequest<Result<List<WorkflowDefinitionDto>>>;

public record WorkflowDefinitionDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    bool IsActive,
    DateTime CreatedAt,
    List<StageDefinitionDto> Stages);

public record StageDefinitionDto(
    int Order,
    string StageName,
    string RoleRequired,
    int SlaDays);
