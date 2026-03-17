using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Common;
using WorkflowService.Application.Commands.CreateWorkflowDefinition;
using WorkflowService.Application.Queries.GetWorkflowDefinitions;

namespace WorkflowService.API.Controllers;

[ApiController]
[Route("api/workflow/definitions")]
[Authorize]
public sealed class WorkflowDefinitionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;

    public WorkflowDefinitionController(
        IMediator mediator,
        ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _tenantContext = tenantContext;
    }

    // POST /api/workflow/definitions
    // Tenant admin creates reusable workflow template
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkflowDefinitionRequest request,
        CancellationToken ct)
    {
        var command = new CreateWorkflowDefinitionCommand(
            TenantId:    _tenantContext.TenantId,
            Name:        request.Name,
            Description: request.Description,
            Stages:      request.Stages.Select(s =>
                new StageDefinitionRequest(
                    s.Order,
                    s.StageName,
                    s.RoleRequired,
                    s.SlaDays)).ToList());

        var result = await _mediator.Send(command, ct);

        if (result.IsFailure)
            return BadRequest(new
            {
                error = result.Error.Description
            });

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new { id = result.Value });
    }

    // GET /api/workflow/definitions
    // List all active templates for this tenant
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetWorkflowDefinitionsQuery(
                _tenantContext.TenantId),
            ct);

        return Ok(result.Value);
    }

    // GET /api/workflow/definitions/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetWorkflowDefinitionsQuery(
                _tenantContext.TenantId),
            ct);

        var definition = result.Value
            .FirstOrDefault(d => d.Id == id);

        if (definition is null)
            return NotFound();

        return Ok(definition);
    }
}

public record CreateWorkflowDefinitionRequest(
    string Name,
    string Description,
    List<StageRequest> Stages);

public record StageRequest(
    int Order,
    string StageName,
    string RoleRequired,
    int SlaDays);
