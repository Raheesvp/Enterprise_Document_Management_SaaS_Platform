using DocumentService.Application.DTOs;
using DocumentService.Domain.Errors;
using DocumentService.Domain.Repositories;
using MediatR;
using Shared.Domain.Common;

namespace DocumentService.Application.Queries.GetDocument;

// Query handler uses IDocumentReadRepository — NOT IDocumentRepository
//
// Why Dapper here and not EF Core?
// → EF Core loads the FULL aggregate including all versions
// → For a detail view we need metadata — not 50 versions of change tracking
// → Dapper runs a single flat SQL query — much faster
// → No change tracking overhead — reads are truly read-only
//
// Real world: Microsoft docs explicitly recommend this split
// for read-heavy endpoints (document list, search results, dashboards)
public sealed class GetDocumentQueryHandler
    : IRequestHandler<GetDocumentQuery, Result<DocumentDto>>
{
    private readonly IDocumentReadRepository _readRepo;

    public GetDocumentQueryHandler(IDocumentReadRepository readRepo)
        => _readRepo = readRepo;

    public async Task<Result<DocumentDto>> Handle(
        GetDocumentQuery query,
        CancellationToken cancellationToken)
    {
        // Dapper read — lightweight, no change tracking
        var summary = await _readRepo.GetSummaryByIdAsync(
            query.DocumentId,
            query.TenantId,
            cancellationToken);

        if (summary is null)
            return Result.Failure<DocumentDto>(
                DocumentErrors.Document.NotFound(query.DocumentId));

        return Result.Success(new DocumentDto(
            summary.Id,
            summary.TenantId,
            summary.Title,
            summary.Status,
            summary.DocumentType,
            summary.MimeType,
            summary.FileSizeBytes,
            FormatFileSize(summary.FileSizeBytes),
            summary.VersionCount,
            summary.VersionCount, // current version = latest count
            string.Empty,         // StoragePath not exposed in list view
            summary.UploadedByUserId,
            summary.CreatedAt,
            summary.UpdatedAt,
            summary.Description,
            summary.Tags));
    }

    // Format bytes into human readable string
    // 1048576 → "1.00 MB"
    // 512     → "0.50 KB"
    private static string FormatFileSize(long bytes)
    {
        return bytes switch
        {
            >= 1024 * 1024 =>
                $"{bytes / (1024.0 * 1024.0):F2} MB",
            >= 1024 =>
                $"{bytes / 1024.0:F2} KB",
            _ =>
                $"{bytes} B"
        };
    }
}