using DocumentService.Domain.Entities;
using DocumentService.Domain.Repositories;

namespace DocumentService.Infrastructure.Stubs;

public sealed class StubDocumentRepository : IDocumentRepository
{
    public Task<Document?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => Task.FromResult<Document?>(null);

    public Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<Document?> GetByTitleAsync(string title, Guid tenantId, CancellationToken ct = default)
        => Task.FromResult<Document?>(null);

    public Task AddAsync(Document document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task UpdateAsync(Document document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteAsync(Document document, CancellationToken ct = default)
        => Task.CompletedTask;
}
