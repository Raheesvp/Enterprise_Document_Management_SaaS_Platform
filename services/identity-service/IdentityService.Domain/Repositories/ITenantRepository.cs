
using IdentityService.Domain.Entities;

namespace IdentityService.Domain.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Tenant?> GetBySubdomainAsync(
        string subdomain,
        CancellationToken ct = default);

    Task<bool> SubdomainExistsAsync(
        string subdomain,
        CancellationToken ct = default);

    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    Task UpdateAsync(Tenant tenant, CancellationToken ct = default);
}