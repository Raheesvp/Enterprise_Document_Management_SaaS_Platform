using DocumentService.Application.DTOs;
using DocumentService.Domain.Errors;
using DocumentService.Domain.Repositories;
using MediatR;
using Shared.Domain.Common;

namespace DocumentService.Application.Queries.GetDocumentVersions;

public sealed class GetDocumentVersionsQueryHandler
    : IRequestHandler<GetDocumentVersionsQuery, Result<IReadOnlyList<DocumentVersionDto>>>
{
    private readonly IDocumentReadRepository _readRepo;

    public GetDocumentVersionsQueryHandler(
        IDocumentReadRepository readRepo)
        => _readRepo = readRepo;

    public async Task<Result<IReadOnlyList<DocumentVersionDto>>> Handle(
        GetDocumentVersionsQuery query,
        CancellationToken cancellationToken)
    {
        // First confirm document exists for this tenant
        var document = await _readRepo.GetSummaryByIdAsync(
            query.DocumentId,
            query.TenantId,
            cancellationToken);

        if (document is null)
            return Result.Failure<IReadOnlyList<DocumentVersionDto>>(
                DocumentErrors.Document.NotFound(query.DocumentId));

        // Fetch all versions — ordered by VersionNumber ascending
        var versions = await _readRepo.GetVersionsAsync(
            query.DocumentId,
            query.TenantId,
            cancellationToken);

        var dtos = versions
            .Select(v => new DocumentVersionDto(
                v.Id,
                v.VersionNumber,
                v.FileSizeBytes,
                FormatFileSize(v.FileSizeBytes),
                v.StoragePath,
                v.IsCurrentVersion,
                v.UploadedByUserId,
                v.CreatedAt,
                v.ExtractedText,
                v.PageCount))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<DocumentVersionDto>>(dtos);
    }

    private static string FormatFileSize(long bytes) =>
        bytes switch
        {
            >= 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F2} MB",
            >= 1024        => $"{bytes / 1024.0:F2} KB",
            _              => $"{bytes} B"
        };
}
