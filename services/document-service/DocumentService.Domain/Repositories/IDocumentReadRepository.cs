using DocumentService.Domain.Enums;

namespace DocumentService.Domain.Repositories;

// Read-side repository — Queries use this via Dapper
// Returns lightweight read models — NOT full aggregates
// No EF Core change tracking overhead on reads
//
// Senior pattern: CQRS split — reads never load full object graph
// A document list page needs Title + Status + Date, NOT all 50 versions
public interface IDocumentReadRepository
{
    Task<DocumentSummary?> GetSummaryByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);

    Task<PagedResult<DocumentSummary>> GetPagedAsync(
        Guid tenantId,
        DocumentQueryFilter filter,
        CancellationToken ct = default);

    Task<IReadOnlyList<DocumentVersionSummary>> GetVersionsAsync(
        Guid documentId,
        Guid tenantId,
        CancellationToken ct = default);
}

// ── Read Models ────────────────────────────────────────────────
// These are NOT domain entities — they are flat projections
// Optimized for what the UI actually needs to display

public record DocumentSummary(
    Guid Id,
    Guid TenantId,
    string Title,
    string Status,
    string DocumentType,
    string MimeType,
    long FileSizeBytes,
    string StoragePath,
    int VersionCount,
    Guid UploadedByUserId,
    string UploadedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Description,
    string? Tags);

public record DocumentVersionSummary(
    Guid Id,
    int VersionNumber,
    long FileSizeBytes,
    string StoragePath,
    bool IsCurrentVersion,
    Guid UploadedByUserId,
    string UploadedByName,
    DateTime CreatedAt,
    string? ExtractedText,
    int? PageCount);

// ── Query Parameters ───────────────────────────────────────────
public record DocumentQueryFilter(
    DocumentStatus? Status = null,
    DocumentType? Type = null,
    string? SearchTerm = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20);

// ── Paged Result Wrapper ───────────────────────────────────────
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages =>
        (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
