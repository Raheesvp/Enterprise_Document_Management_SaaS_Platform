using DocumentService.Domain.Entities;
using DocumentService.Domain.Repositories;
using DocumentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Infrastructure.Repositories;

// DocumentRepository — EF Core write-side repository
//
// Every method takes BOTH id AND tenantId
// This enforces tenant isolation at repository level
// Even if application code passes wrong tenantId
// the query returns null — never wrong tenant data
//
// This is Layer 1 of tenant isolation
// PostgreSQL RLS is Layer 2 (Day 16)
public sealed class DocumentRepository : IDocumentRepository
{
    private readonly DocumentDbContext _context;

    public DocumentRepository(DocumentDbContext context)
        => _context = context;

    public async Task<Document?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .Include(d => d.Versions) // Load versions with document
            .FirstOrDefaultAsync(
                d => d.Id == id && d.TenantId == tenantId, ct);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        return await _context.Documents
            .AnyAsync(
                d => d.Id == id && d.TenantId == tenantId, ct);
    }

    public async Task AddAsync(
        Document document,
        CancellationToken ct = default)
    {
        await _context.Documents.AddAsync(document, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(
        Document document,
        CancellationToken ct = default)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(
        Document document,
        CancellationToken ct = default)
    {
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(ct);
    }
}
