using NotificationService.Application.DTOs;

namespace NotificationService.Application.Interfaces;

public interface INotificationBroadcaster
{
    Task BroadcastToUserAsync(
        string userId,
        NotificationDto notification,
        CancellationToken ct = default);

    Task BroadcastUnreadCountAsync(
        string userId,
        int unreadCount,
        CancellationToken ct = default);

    Task BroadcastToTenantAsync(
        string tenantId,
        NotificationDto notification,
        CancellationToken ct = default);
}
