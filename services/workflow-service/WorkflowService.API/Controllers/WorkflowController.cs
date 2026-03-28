using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Common;
using WorkflowService.Application.Commands.ApproveStage;
using WorkflowService.Application.Commands.RejectStage;
using WorkflowService.Application.Commands.StartWorkflow;
using WorkflowService.Application.Queries.GetWorkflowStatus;
using WorkflowService.Application.Interfaces;

namespace WorkflowService.API.Controllers;

[ApiController]
[Route("api/workflow")]
[Authorize]
public sealed class WorkflowController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;
    private readonly IWorkflowRepository _repository;

    public WorkflowController(
        IMediator mediator,
        ITenantContext tenantContext,
        IWorkflowRepository repository)
    {
        _mediator      = mediator;
        _tenantContext = tenantContext;
        _repository    = repository;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartWorkflow(
        [FromBody] StartWorkflowCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            command with { TenantId = _tenantContext.TenantId }, ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return CreatedAtAction(
            nameof(GetStatus),
            new { id = result.Value.Id },
            result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStatus(
        Guid id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetWorkflowStatusQuery(id, _tenantContext.TenantId), ct);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpGet("document/{documentId:guid}")]
    public async Task<IActionResult> GetByDocumentId(
        Guid documentId,
        CancellationToken ct)
    {
        var workflow = await _repository
            .GetByDocumentIdAsync(documentId, _tenantContext.TenantId, ct);

        if (workflow is null)
            return NotFound(new { error = "No workflow found for this document" });

        return Ok(new
        {
            id                = workflow.Id,
            tenantId          = workflow.TenantId,
            documentId        = workflow.DocumentId,
            documentTitle     = workflow.DocumentTitle,
            status            = workflow.Status.ToString(),
            currentStageOrder = workflow.CurrentStageOrder,
            initiatedByUserId = workflow.InitiatedByUserId,
            startedAt         = workflow.StartedAt,
            completedAt       = workflow.CompletedAt,
            stages            = workflow.Stages.Select(s => new
            {
                id               = s.Id,
                stageOrder       = s.StageOrder,
                stageName        = s.StageName,
                assignedToUserId = s.AssignedToUserId,
                assignedToEmail  = s.AssignedToEmail,
                status           = s.Status.ToString(),
                comments         = s.Comments,
                slaDeadline      = s.SlaDeadline,
                processedAt      = s.ProcessedAt,
            }).ToList()
        });
    }

    // GET /api/workflow/my - list all workflows for this tenant
    [HttpGet("my")]
    public async Task<IActionResult> GetMyWorkflows(
        CancellationToken ct)
    {
        var userId = GetUserId(); // Use your existing helper
    var allWorkflows = await _repository.GetByTenantAsync(_tenantContext.TenantId, ct);
    
    // Filter by the user who initiated it
    var myWorkflows = allWorkflows.Where(w => w.InitiatedByUserId == userId);

        var dtos = myWorkflows.Select(w => new
        {
            id                = w.Id,
            tenantId          = w.TenantId,
            documentId        = w.DocumentId,
            documentTitle     = w.DocumentTitle,
            status            = w.Status.ToString(),
            currentStageOrder = w.CurrentStageOrder,
            initiatedByUserId = w.InitiatedByUserId,
            startedAt         = w.StartedAt,
            completedAt       = w.CompletedAt,
            stages            = w.Stages.Select(s => new
            {
                id               = s.Id,
                stageOrder       = s.StageOrder,
                stageName        = s.StageName,
                assignedToUserId = s.AssignedToUserId,
                assignedToEmail  = s.AssignedToEmail,
                status           = s.Status.ToString(),
                comments         = s.Comments,
                slaDeadline      = s.SlaDeadline,
                processedAt      = s.ProcessedAt,
            }).ToList()
        });

        return Ok(dtos);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveStage(
        Guid id,
        [FromBody] ApproveStageRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ApproveStageCommand(
                id, _tenantContext.TenantId,
                GetUserId(), request.Comments), ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectStage(
        Guid id,
        [FromBody] RejectStageRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new RejectStageCommand(
                id, _tenantContext.TenantId,
                GetUserId(), request.Comments), ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value
                 ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public record ApproveStageRequest(string? Comments);
public record RejectStageRequest(string? Comments);
