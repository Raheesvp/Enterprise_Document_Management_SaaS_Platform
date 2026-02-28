
using Shared.Domain.Primitives;

namespace IdentityService.Domain.Entities;

public sealed class RefreshToken : BaseEntity<Guid>
{
    private RefreshToken() { } // EF Core

    public RefreshToken(
        Guid userId,
        string token,
        DateTime expiresAt) : base(Guid.NewGuid())
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
    }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? RevokedReason { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string reason)
    {
        IsRevoked = true;
        RevokedReason = reason;
        RevokedAt = DateTime.UtcNow;
    }
}