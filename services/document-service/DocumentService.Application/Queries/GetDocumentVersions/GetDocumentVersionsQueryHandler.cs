using DocumentService.Application.DTOs;
using DocumentService.Application.Interfaces;
using DocumentService.Domain.Errors;
using DocumentService.Domain.Repositories;
using MediatR;
using Shared.Domain.Common;

namespace DocumentService.Application.Queries.GetDocumentVersions;

public sealed class GetDocumentVersionsQueryHandler
    : IRequestHandler<GetDocumentVersionsQuery, Result<IReadOnlyList<DocumentVersionDto>>>
{
    private readonly IDocumentReadRepository _readRepo;
    private readonly IStorageService _storageService;

    public GetDocumentVersionsQueryHandler(
        IDocumentReadRepository readRepo,
        IStorageService storageService)
    {
        _readRepo = readRepo;
        _storageService = storageService;
    }

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

        var dtos = new List<DocumentVersionDto>();
        
        foreach (var v in versions)
        {
            var downloadUrl = await _storageService.GetPresignedUrlAsync(
                v.StoragePath,
                document.MimeType);

            dtos.Add(new DocumentVersionDto(
                v.Id,
                v.VersionNumber,
                v.FileSizeBytes,
                FormatFileSize(v.FileSizeBytes),
                v.StoragePath,
                v.IsCurrentVersion,
                v.UploadedByUserId.ToString(),
                v.CreatedAt,
                v.ExtractedText,
                v.PageCount,
                downloadUrl));
        }

        return Result.Success<IReadOnlyList<DocumentVersionDto>>(dtos.AsReadOnly());
    }

    private static string FormatFileSize(long bytes) =>
        bytes switch
        {
            >= 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F2} MB",
            >= 1024        => $"{bytes / 1024.0:F2} KB",
            _              => $"{bytes} B"
        };
}
