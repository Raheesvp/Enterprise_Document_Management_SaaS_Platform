using MediatR;
using Shared.Domain.Common;

namespace WorkflowService.Application.Commands.CreateWorkflowDefinition;

// CreateWorkflowDefinitionCommand — tenant admin creates
// a reusable approval template
//
// Example:
// Tenant "Acme Corp" creates template:
//   Name: "Contract Approval"
//   Stage 1: Manager Review  (2 days SLA)
//   Stage 2: Legal Review    (3 days SLA)
//   Stage 3: CFO Sign-off    (1 day SLA)
//
// Every contract uploaded by Acme Corp
// automatically uses this 3-stage template
public record CreateWorkflowDefinitionCommand(
    Guid TenantId,
    string Name,
    string Description,
    List<StageDefinitionRequest> Stages)
    : IRequest<Result<Guid>>;

public record StageDefinitionRequest(
    int Order,
    string StageName,
    string RoleRequired,
    int SlaDays);
