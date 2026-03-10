using DocumentService.Application.DTOs;
using DocumentService.Domain.Repositories;
using MediatR;
using Shared.Domain.Common;

namespace DocumentService.Application.Queries.GetDocumentList;

public sealed class GetDocumentListQueryHandler
    : IRequestHandler<GetDocumentListQuery, Result<DocumentListDto>>
{
    private readonly IDocumentReadRepository _readRepo;

    public GetDocumentListQueryHandler(
        IDocumentReadRepository readRepo)
        => _readRepo = readRepo;

    public async Task<Result<DocumentListDto>> Handle(
        GetDocumentListQuery query,
        CancellationToken cancellationToken)
    {
        // Build filter object — passed to Dapper repository
        // Dapper translates this into a parameterized SQL WHERE clause
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
        var items = paged.Items
            .Select(s => new DocumentSummaryDto(
                s.Id,
                s.Title,
                s.Status,
                s.DocumentType,
                s.FileSizeBytes,
                FormatFileSize(s.FileSizeBytes),
                s.VersionCount,
                s.CreatedAt,
                s.UpdatedAt,
                s.Tags))
            .ToList()
            .AsReadOnly();

        return Result.Success(new DocumentListDto(
            items,
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