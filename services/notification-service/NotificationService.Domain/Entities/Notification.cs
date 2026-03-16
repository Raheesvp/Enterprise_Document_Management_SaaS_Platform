using NotificationService.Domain.Enums;
using Shared.Domain.Primitives;

namespace NotificationService.Domain.Entities;

// Notification — in-app notification stored in PostgreSQL
// Created when workflow events occur
// Read by frontend via GET /api/notifications
public sealed class Notification : BaseEntity<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationStatus Status { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? ReferenceType { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private Notification(Guid id) : base(id) { }
    private Notification() { }

    public static Notification Create(
        Guid tenantId,
        Guid userId,
        string title,
        string message,
        NotificationType type,
        Guid? referenceId = null,
        string? referenceType = null)
    {
        return new Notification(Guid.NewGuid())
        {
            TenantId      = tenantId,
            UserId        = userId,
            Title         = title,
            Message       = message,
            Type          = type,
            Status        = NotificationStatus.Unread,
            ReferenceId   = referenceId,
            ReferenceType = referenceType,
            CreatedAt     = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        Status = NotificationStatus.Read;
        ReadAt = DateTime.UtcNow;
    }

    public void Archive()
        => Status = NotificationStatus.Archived;
}
