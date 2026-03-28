namespace DocumentService.Application.DTOs;

// DocumentDto — full document detail response
// Returned by: UploadDocument, UpdateDocument, GetDocument
public record DocumentDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string Status,
    string DocumentType,
    string MimeType,
    long FileSizeBytes,
    string FileSizeFormatted,
    int VersionCount,
    int CurrentVersionNumber,
    string StoragePath,
    string UploadedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Description,
    string? Tags,
    string? DownloadUrl = null);

// DocumentVersionDto — single version detail
// Returned by: AddDocumentVersion, GetDocumentVersions
public record DocumentVersionDto(
    Guid Id,
    int VersionNumber,
    long FileSizeBytes,
    string FileSizeFormatted,
    string StoragePath,
    bool IsCurrentVersion,
    string UploadedByUserId,
    DateTime CreatedAt,
    string? ExtractedText,
    int? PageCount,
    string? DownloadUrl = null);

// DocumentSummaryDto — lightweight for list views
// Returned by: GetDocumentList
public record DocumentSummaryDto(
    Guid Id,
    string Title,
    string Status,
    string DocumentType,
    string MimeType,
    long FileSizeBytes,
    string FileSizeFormatted,
    int VersionCount,
    string UploadedByUserId,
    string UploadedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Tags,
    string? DownloadUrl = null);

// DocumentListDto — paged list response
// Returned by: GetDocumentList
public record DocumentListDto(
    IReadOnlyList<DocumentSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);