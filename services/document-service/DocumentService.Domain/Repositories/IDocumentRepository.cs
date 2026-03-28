using DocumentService.Domain.Entities;

namespace DocumentService.Domain.Repositories;

// Write-side repository — Commands use this via EF Core
// Only the operations that CHANGE data live here
// Read operations live in IDocumentReadRepository (Dapper)
//
// GetByIdAsync takes BOTH documentId AND tenantId
// This enforces tenant isolation at the repository contract level
// Infrastructure MUST filter by tenantId — no exceptions
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);

    Task<Document?> GetByTitleAsync(
        string title,
        Guid tenantId,
        CancellationToken ct = default);

    Task AddAsync(
        Document document,
        CancellationToken ct = default);

    Task UpdateAsync(
        Document document,
        CancellationToken ct = default);

    Task DeleteAsync(
        Document document,
        CancellationToken ct = default);
}