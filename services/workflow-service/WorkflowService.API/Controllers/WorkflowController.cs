using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowService.Application.Commands.ApproveStage;
using WorkflowService.Application.Commands.RejectStage;
using WorkflowService.Application.Commands.StartWorkflow;
using WorkflowService.Application.Queries.GetWorkflowStatus;
using Shared.Domain.Common;

namespace WorkflowService.API.Controllers;

[ApiController]
[Route("api/workflow")]
[Authorize]
public sealed class WorkflowController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;

    public WorkflowController(
        IMediator mediator,
        ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _tenantContext = tenantContext;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartWorkflow(
        [FromBody] StartWorkflowCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            command with { TenantId = _tenantContext.TenantId },
            ct);

        if (result.IsFailure)
            return BadRequest(new
            {
                error = result.Error.Description
            });

        return CreatedAtAction(
            nameof(GetStatus),
            new { id = result.Value.Id },
            result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStatus(
        Guid id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetWorkflowStatusQuery(
                id, _tenantContext.TenantId),
            ct);

        if (result.IsFailure)
            return NotFound(new
            {
                error = result.Error.Description
            });

        return Ok(result.Value);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveStage(
        Guid id,
        [FromBody] ApproveStageRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(
            new ApproveStageCommand(
                id,
                _tenantContext.TenantId,
                userId,
                request.Comments),
            ct);

        if (result.IsFailure)
            return BadRequest(new
            {
                error = result.Error.Description
            });

        return Ok(result.Value);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectStage(
        Guid id,
        [FromBody] RejectStageRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(
            new RejectStageCommand(
                id,
                _tenantContext.TenantId,
                userId,
                request.Comments),
            ct);

        if (result.IsFailure)
            return BadRequest(new
            {
                error = result.Error.Description
            });

        return Ok(result.Value);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value
                 ?? User.FindFirst("user_id")?.Value;

        return Guid.TryParse(claim, out var id)
            ? id : Guid.Empty;
    }
}

public record ApproveStageRequest(string? Comments);
public record RejectStageRequest(string? Comments);
