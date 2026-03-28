using DocumentService.Application.Commands.AddDocumentVersion;
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
    private readonly IMediator               _mediator;
    private readonly IUploadSessionStore     _sessionStore;
    private readonly ITenantContext          _tenantContext;
    private readonly IStorageService         _storageService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IMediator               mediator,
        IUploadSessionStore     sessionStore,
        ITenantContext          tenantContext,
        IStorageService         storageService,
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
            IsComplete      = false,
            DocumentId      = request.DocumentId
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

        _logger.LogDebug(
            "[CHUNK] UploadId: {UploadId} " +
            "Offset: {Offset} Size: {Size} " +
            "TempPath: {TempPath}",
            uploadId, offset, chunkSize,
            session.TempStoragePath);

        await _sessionStore.UpdateProgressAsync(
            uploadId, newOffset, ct);

        if (newOffset >= session.TotalSize)
        {
            await _sessionStore.CompleteAsync(uploadId, ct);

            _logger.LogInformation(
                "[CHUNK] All chunks received. " +
                "Finalizing UploadId: {UploadId}",
                uploadId);

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
            _logger.LogInformation(
                "[FINALIZE] Starting. UploadId: {UploadId} " +
                "TempPath: {TempPath} Size: {Size}",
                session.UploadId,
                session.TempStoragePath,
                session.TotalSize);

            // Download file from MinIO temp storage
            var fileStream = await _storageService
                .DownloadAsync(session.TempStoragePath, ct);

            _logger.LogInformation(
                "[FINALIZE] File downloaded from MinIO. " +
                "UploadId: {UploadId}",
                session.UploadId);

            if (session.DocumentId.HasValue)
            {
                var versionCommand = new AddDocumentVersionCommand(
                    DocumentId:       session.DocumentId.Value,
                    TenantId:         session.TenantId,
                    UploadedByUserId: session.UserId,
                    FileSizeBytes:    session.TotalSize,
                    FileContent:      fileStream,
                    MimeType:         session.ContentType);

                var versionResult = await _mediator.Send(versionCommand, ct);

                if (versionResult.IsFailure)
                {
                    _logger.LogError(
                        "[FINALIZE] Command failed. " +
                        "UploadId: {UploadId} " +
                        "Error: {Code} - {Description}",
                        session.UploadId,
                        versionResult.Error.Code,
                        versionResult.Error.Description);
                    return;
                }

                _logger.LogInformation(
                    "[FINALIZE] SUCCESS. " +
                    "UploadId: {UploadId} " +
                    "DocumentId: {DocumentId} VersionId: {VersionId} VersionNumber: {VersionNumber}",
                    session.UploadId,
                    session.DocumentId.Value,
                    versionResult.Value.Id,
                    versionResult.Value.VersionNumber);
            }
            else
            {
                var command = new UploadDocumentCommand(
                    TenantId:         session.TenantId,
                    UploadedByUserId: session.UserId,
                    UploadedByName:   GetUserName(),
                    Title: Path.GetFileNameWithoutExtension(
                        session.FileName),
                    MimeType:         session.ContentType,
                    FileSizeBytes:    session.TotalSize,
                    FileContent:      fileStream,
                    DocumentId:       null);

                var result = await _mediator.Send(command, ct);

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "[FINALIZE] Command failed. " +
                        "UploadId: {UploadId} " +
                        "Error: {Code} - {Description}",
                        session.UploadId,
                        result.Error.Code,
                        result.Error.Description);
                    return;
                }

                _logger.LogInformation(
                    "[FINALIZE] SUCCESS. " +
                    "UploadId: {UploadId} " +
                    "DocumentId: {DocumentId}",
                    session.UploadId,
                    result.Value.Id);
            }

            // Cleanup temp file
            try
            {
                await _storageService.DeleteAsync(
                    session.TempStoragePath, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[FINALIZE] Failed to delete temp file. " +
                    "TempPath: {TempPath}",
                    session.TempStoragePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[FINALIZE] EXCEPTION. UploadId: {UploadId}",
                session.UploadId);
        }
    }

    private string GetUserName()
    {
        return User.FindFirst("full_name")?.Value
            ?? User.FindFirst("name")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? "Unknown";
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
    public string  FileName    { get; init; } = string.Empty;
    public string  ContentType { get; init; } = string.Empty;
    public long    TotalSize   { get; init; }
    public Guid?   DocumentId  { get; init; }
}

public sealed record InitUploadResponse
{
    public string UploadId        { get; init; } = string.Empty;
    public string TempStoragePath { get; init; } = string.Empty;
}
