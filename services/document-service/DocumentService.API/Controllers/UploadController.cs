using DocumentService.Application.Commands.UploadDocument;
using DocumentService.Application.Interfaces;
using DocumentService.Infrastructure.Upload;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Common;

namespace DocumentService.API.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize]
public sealed class UploadController : ControllerBase
{
    private readonly IMediator             _mediator;
    private readonly IUploadSessionStore   _sessionStore;
    private readonly ITenantContext        _tenantContext;
    private readonly IStorageService       _storageService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IMediator             mediator,
        IUploadSessionStore   sessionStore,
        ITenantContext        tenantContext,
        IStorageService       storageService,
        ILogger<UploadController> logger)
    {
        _mediator       = mediator;
        _sessionStore   = sessionStore;
        _tenantContext  = tenantContext;
        _storageService = storageService;
        _logger         = logger;
    }

    [HttpPost("init")]
    public async Task<IActionResult> InitUpload(
        [FromBody] InitUploadRequest request,
        CancellationToken ct)
    {
        if (!_tenantContext.IsResolved)
            return Unauthorized("Tenant context not resolved");

        var uploadId = Guid.NewGuid().ToString("N");
        var session  = new UploadSession
        {
            UploadId        = uploadId,
            TenantId        = _tenantContext.TenantId,
            UserId          = GetUserId(),
            FileName        = request.FileName,
            ContentType     = request.ContentType,
            TotalSize       = request.TotalSize,
            TempStoragePath =
                $"temp/{_tenantContext.TenantId}/{uploadId}",
            IsComplete      = false
        };

        await _sessionStore.SaveAsync(session, ct);

        return Ok(new InitUploadResponse
        {
            UploadId        = uploadId,
            TempStoragePath = session.TempStoragePath
        });
    }

    [HttpPost("{uploadId}/chunk")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadChunk(
        string uploadId,
        [FromQuery] long offset,
        CancellationToken ct)
    {
        var session = await _sessionStore.GetAsync(uploadId, ct);

        if (session is null)
            return NotFound(
                $"Upload session not found: {uploadId}");

        if (session.TenantId != _tenantContext.TenantId)
            return Forbid();

        if (session.IsComplete)
            return BadRequest("Upload already completed");

        Request.EnableBuffering();

        using var memStream = new MemoryStream();
        await Request.Body.CopyToAsync(memStream, ct);
        var chunkSize = memStream.Length;
        var newOffset = offset + chunkSize;

        // Store chunk to MinIO temp path
        memStream.Position = 0;
        await _storageService.UploadAsync(
            session.TempStoragePath,
            memStream,
            session.ContentType,
            ct);

        await _sessionStore.UpdateProgressAsync(
            uploadId, newOffset, ct);

        _logger.LogDebug(
            "Chunk received. UploadId: {UploadId} " +
            "Offset: {Offset} ChunkSize: {ChunkSize}",
            uploadId, offset, chunkSize);

        if (newOffset >= session.TotalSize)
        {
            await _sessionStore.CompleteAsync(uploadId, ct);
            await FinalizeUploadAsync(session, ct);

            return Ok(new
            {
                Status   = "Complete",
                UploadId = uploadId
            });
        }

        return Ok(new
        {
            Status          = "ChunkReceived",
            UploadId        = uploadId,
            BytesReceived   = newOffset,
            TotalSize       = session.TotalSize,
            PercentComplete = (int)((double)newOffset
                / session.TotalSize * 100)
        });
    }

    [HttpGet("{uploadId}/status")]
    public async Task<IActionResult> GetStatus(
        string uploadId,
        CancellationToken ct)
    {
        var session = await _sessionStore.GetAsync(uploadId, ct);

        if (session is null)
            return NotFound(
                $"Upload session not found: {uploadId}");

        if (session.TenantId != _tenantContext.TenantId)
            return NotFound(
                $"Upload session not found: {uploadId}");

        return Ok(new
        {
            UploadId        = session.UploadId,
            FileName        = session.FileName,
            TotalSize       = session.TotalSize,
            BytesReceived   = session.BytesReceived,
            IsComplete      = session.IsComplete,
            PercentComplete = session.TotalSize > 0
                ? (int)((double)session.BytesReceived
                    / session.TotalSize * 100)
                : 0
        });
    }

    [HttpDelete("{uploadId}")]
    public async Task<IActionResult> CancelUpload(
        string uploadId,
        CancellationToken ct)
    {
        var session = await _sessionStore.GetAsync(uploadId, ct);

        if (session is null)
            return NotFound(
                $"Upload session not found: {uploadId}");

        if (session.TenantId != _tenantContext.TenantId)
            return NotFound(
                $"Upload session not found: {uploadId}");

        await _sessionStore.DeleteAsync(uploadId, ct);
        return NoContent();
    }

    private async Task FinalizeUploadAsync(
        UploadSession session,
        CancellationToken ct)
    {
        try
        {
            // Download file from MinIO temp storage
            // This is the file that was uploaded in chunks
            var fileStream = await _storageService
                .DownloadAsync(session.TempStoragePath, ct);

            var command = new UploadDocumentCommand(
                TenantId:         session.TenantId,
                UploadedByUserId: session.UserId,
                Title: Path.GetFileNameWithoutExtension(
                    session.FileName),
                MimeType:         session.ContentType,
                FileSizeBytes:    session.TotalSize,
                FileContent:      fileStream);

            var result = await _mediator.Send(command, ct);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "Failed to finalize upload. " +
                    "UploadId: {UploadId} " +
                    "Error: {Code} - {Description}",
                    session.UploadId,
                    result.Error.Code,
                    result.Error.Description);
            }
            else
            {
                _logger.LogInformation(
                    "Upload finalized successfully. " +
                    "UploadId: {UploadId} " +
                    "DocumentId: {DocumentId}",
                    session.UploadId,
                    result.Value.Id);

                // Cleanup temp file after finalization
                await _storageService.DeleteAsync(
                    session.TempStoragePath, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception during upload finalization. " +
                "UploadId: {UploadId}",
                session.UploadId);
        }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value
                 ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var id)
            ? id : Guid.Empty;
    }
}

public sealed record InitUploadRequest
{
    public string FileName    { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long   TotalSize   { get; init; }
}

public sealed record InitUploadResponse
{
    public string UploadId        { get; init; } = string.Empty;
    public string TempStoragePath { get; init; } = string.Empty;
}
