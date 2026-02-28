namespace shared.Domain.Common;

public interface ITenantContext
{
    Guid TenantId { get; }

    string TenantName { get; }

    bool IsResolved { get; }
}