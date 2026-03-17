using Microsoft.EntityFrameworkCore;
using WorkflowService.Application.Interfaces;
using WorkflowService.Domain.Entities;
using WorkflowService.Infrastructure.Persistence;

namespace WorkflowService.Infrastructure.Repositories;

public sealed class WorkflowRepository : IWorkflowRepository
{
    private readonly WorkflowDbContext _context;

    public WorkflowRepository(WorkflowDbContext context)
        => _context = context;

    public async Task<WorkflowInstance?> GetByIdAsync(
        Guid id, Guid tenantId,
        CancellationToken ct = default)
    {
        return await _context.WorkflowInstances
            .Include(w => w.Stages)
            .FirstOrDefaultAsync(
                w => w.Id == id && w.TenantId == tenantId, ct);
    }

    public async Task<WorkflowInstance?> GetByDocumentIdAsync(
        Guid documentId, Guid tenantId,
        CancellationToken ct = default)
    {
        return await _context.WorkflowInstances
            .Include(w => w.Stages)
            .FirstOrDefaultAsync(
                w => w.DocumentId == documentId
                  && w.TenantId == tenantId, ct);
    }

    public async Task AddAsync(
        WorkflowInstance instance,
        CancellationToken ct = default)
    {
        await _context.WorkflowInstances.AddAsync(instance, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(
        WorkflowInstance instance,
        CancellationToken ct = default)
    {
        _context.WorkflowInstances.Update(instance);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<WorkflowDefinition?> GetDefinitionByIdAsync(
        Guid id, Guid tenantId,
        CancellationToken ct = default)
    {
        return await _context.WorkflowDefinitions
            .Include(d => d.Stages)
            .FirstOrDefaultAsync(
                d => d.Id == id && d.TenantId == tenantId, ct);
    }

    public async Task AddDefinitionAsync(
        WorkflowDefinition definition,
        CancellationToken ct = default)
    {
        await _context.WorkflowDefinitions
            .AddAsync(definition, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>>
        GetDefinitionsByTenantAsync(
            Guid tenantId,
            CancellationToken ct = default)
    {
        return await _context.WorkflowDefinitions
            .Include(d => d.Stages)
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }
}
