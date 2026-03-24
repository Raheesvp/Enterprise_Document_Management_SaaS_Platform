using Microsoft.AspNetCore.SignalR;
using NotificationService.Application.Interfaces;
using NotificationService.Application.DTOs;

namespace NotificationService.Infrastructure.Services;

// Marker hub — avoids circular dependency between
// Infrastructure and API projects
public class NotificationMarkerHub : Hub {}

public sealed class NotificationBroadcaster
    : INotificationBroadcaster
{
    private readonly IHubContext<NotificationMarkerHub> _hubContext;

    public NotificationBroadcaster(
        IHubContext<NotificationMarkerHub> hubContext)
        => _hubContext = hubContext;

    public async Task BroadcastToUserAsync(
        string userId,
        NotificationDto notification,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("NewNotification", notification,
                cancellationToken: ct);
    }

    public async Task BroadcastUnreadCountAsync(
        string userId,
        int unreadCount,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("UnreadCountUpdated", unreadCount,
                cancellationToken: ct);
    }

    public async Task BroadcastToTenantAsync(
        string tenantId,
        NotificationDto notification,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"tenant-{tenantId}")
            .SendAsync("NewNotification", notification,
                cancellationToken: ct);
    }
}
