using WorkflowService.Domain.Entities;

namespace WorkflowService.Application.Interfaces;

public interface IWorkflowRepository
{
    Task<WorkflowInstance?> GetByIdAsync(
        Guid id, Guid tenantId,
        CancellationToken ct = default);

    Task<WorkflowInstance?> GetByDocumentIdAsync(
        Guid documentId, Guid tenantId,
        CancellationToken ct = default);

    Task AddAsync(
        WorkflowInstance instance,
        CancellationToken ct = default);

    Task UpdateAsync(
        WorkflowInstance instance,
        CancellationToken ct = default);

    Task<WorkflowDefinition?> GetDefinitionByIdAsync(
        Guid id, Guid tenantId,
        CancellationToken ct = default);

    Task AddDefinitionAsync(
        WorkflowDefinition definition,
        CancellationToken ct = default);

    // New — list all definitions for a tenant
    Task<IReadOnlyList<WorkflowDefinition>> GetDefinitionsByTenantAsync(
        Guid tenantId,
        CancellationToken ct = default);
}
