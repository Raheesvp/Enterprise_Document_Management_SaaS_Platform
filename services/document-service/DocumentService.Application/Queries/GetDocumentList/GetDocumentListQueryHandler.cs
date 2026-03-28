using DocumentService.Application.DTOs;
using DocumentService.Application.Interfaces;
using DocumentService.Domain.Repositories;
using MediatR;
using Shared.Domain.Common;

namespace DocumentService.Application.Queries.GetDocumentList;

public sealed class GetDocumentListQueryHandler
    : IRequestHandler<GetDocumentListQuery, Result<DocumentListDto>>
{
    private readonly IDocumentReadRepository _readRepo;
    private readonly IStorageService _storageService;

    public GetDocumentListQueryHandler(
        IDocumentReadRepository readRepo,
        IStorageService storageService)
    {
        _readRepo = readRepo;
        _storageService = storageService;
    }

    public async Task<Result<DocumentListDto>> Handle(
        GetDocumentListQuery query,
        CancellationToken cancellationToken)
    {
        // Build filter object — passed to Dapper repository
        var filter = new DocumentQueryFilter(
            Status:     query.Status,
            Type:       query.Type,
            SearchTerm: query.SearchTerm,
            FromDate:   query.FromDate,
            ToDate:     query.ToDate,
            Page:       query.Page,
            PageSize:   query.PageSize);

        var paged = await _readRepo.GetPagedAsync(
            query.TenantId,
            filter,
            cancellationToken);

        // Map read models → DTOs
        var itemsList = new List<DocumentSummaryDto>();
        
        foreach (var s in paged.Items)
        {
            var downloadUrl = await _storageService.GetPresignedUrlAsync(
                s.StoragePath, 
                s.MimeType);

            itemsList.Add(new DocumentSummaryDto(
                s.Id,
                s.Title,
                s.Status,
                s.DocumentType,
                s.MimeType,
                s.FileSizeBytes,
                FormatFileSize(s.FileSizeBytes),
                s.VersionCount,
                s.UploadedByUserId.ToString(),
                s.UploadedByName,
                s.CreatedAt,
                s.UpdatedAt,
                s.Tags,
                downloadUrl));
        }

        return Result.Success(new DocumentListDto(
            itemsList.AsReadOnly(),
            paged.TotalCount,
            paged.Page,
            paged.PageSize,
            paged.TotalPages,
            paged.HasNextPage,
            paged.HasPreviousPage));
    }

    private static string FormatFileSize(long bytes) =>
        bytes switch
        {
            >= 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F2} MB",
            >= 1024        => $"{bytes / 1024.0:F2} KB",
            _              => $"{bytes} B"
        };
}