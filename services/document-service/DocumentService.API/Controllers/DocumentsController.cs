using DocumentService.Application.Commands.ArchiveDocument;
using DocumentService.Application.Interfaces;
using DocumentService.Application.Queries.GetDocument;
using DocumentService.Application.Queries.GetDocumentList;
using DocumentService.Application.Queries.GetDocumentVersions;
using DocumentService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Common;

namespace DocumentService.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;
    private readonly IStorageService _storageService;

    public DocumentsController(
        IMediator mediator,
        ITenantContext tenantContext,
        IStorageService storageService)
    {
        _mediator      = mediator;
        _tenantContext = tenantContext;
        _storageService = storageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        DocumentStatus? documentStatus = null;
        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<DocumentStatus>(status, out var parsed))
        {
            documentStatus = parsed;
        }

        var query = new GetDocumentListQuery(
            TenantId:   _tenantContext.TenantId,
            Status:     documentStatus,
            SearchTerm: searchTerm,
            Page:       page,
            PageSize:   pageSize);

        var result = await _mediator.Send(query, ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDocument(
        Guid id,
        CancellationToken ct)
    {
        var query = new GetDocumentQuery(
            DocumentId: id,
            TenantId:   _tenantContext.TenantId);

        var result = await _mediator.Send(query, ct);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersions(
        Guid id,
        CancellationToken ct)
    {
        var query = new GetDocumentVersionsQuery(
            DocumentId: id,
            TenantId:   _tenantContext.TenantId);

        var result = await _mediator.Send(query, ct);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken ct)
    {
        var command = new ArchiveDocumentCommand(
            DocumentId:        id,
            TenantId:          _tenantContext.TenantId,
            RequestedByUserId: GetUserId());

        var result = await _mediator.Send(command, ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return NoContent();
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken ct)
    {
        var query = new GetDocumentQuery(
            DocumentId: id,
            TenantId:   _tenantContext.TenantId);

        var result = await _mediator.Send(query, ct);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        var doc = result.Value;

        if (string.IsNullOrWhiteSpace(doc.StoragePath))
            return NotFound(new { error = "Document storage path not found" });

        try
        {
            var fileStream = await _storageService.DownloadAsync(
                doc.StoragePath, ct);

            return File(
                fileStream,
                doc.MimeType,
                BuildDownloadFileName(doc.Title, doc.MimeType));
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "Physical file not found" });
        }
    }

    private static string BuildDownloadFileName(
        string title,
        string mimeType)
    {
        if (Path.HasExtension(title))
            return title;

        var extension = mimeType.ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "text/plain" => ".txt",
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            _ => string.Empty
        };

        return $"{title}{extension}";
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value
                 ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var id)
            ? id : Guid.Empty;
    }
}
