using Microsoft.AspNetCore.Http;
using Shared.Domain.Common;

namespace NotificationService.Infrastructure.Services;

public sealed class HttpTenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public string TenantName { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }

    public HttpTenantContext(
        IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return;

        var tenantHeader = context.Request
            .Headers["X-Tenant-Id"].FirstOrDefault();

        if (Guid.TryParse(tenantHeader, out var tenantId))
        {
            TenantId   = tenantId;
            IsResolved = true;
            TenantName = context.User
                .FindFirst("tenant_name")?.Value
                ?? string.Empty;
            return;
        }

        var tenantClaim = context.User
            .FindFirst("tenant_id")?.Value;

        if (Guid.TryParse(tenantClaim, out var claimTenantId))
        {
            TenantId   = claimTenantId;
            TenantName = context.User
                .FindFirst("tenant_name")?.Value
                ?? string.Empty;
            IsResolved = true;
        }
    }
}
