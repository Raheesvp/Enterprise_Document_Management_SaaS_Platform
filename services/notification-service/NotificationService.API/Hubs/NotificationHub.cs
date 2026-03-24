using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Infrastructure.Services;

namespace NotificationService.API.Hubs;

[Authorize]
public sealed class NotificationHub : NotificationMarkerHub
{
    public override async Task OnConnectedAsync()
    {
        var userId   = GetUserId();
        var tenantId = GetTenantId();

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId, $"user-{userId}");
            await Groups.AddToGroupAsync(
                Context.ConnectionId, $"tenant-{tenantId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId, $"user-{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task MarkAsRead(string notificationId)
    {
        var userId = GetUserId();
        await Clients.Group($"user-{userId}")
            .SendAsync("NotificationRead", notificationId);
    }

    private string GetUserId() =>
        Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(
                System.Security.Claims.ClaimTypes
                    .NameIdentifier)?.Value
            ?? string.Empty;

    private string GetTenantId() =>
        Context.User?.FindFirst("tenant_id")?.Value
            ?? string.Empty;
}
