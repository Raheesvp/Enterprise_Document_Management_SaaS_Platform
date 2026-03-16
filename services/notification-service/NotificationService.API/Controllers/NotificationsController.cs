using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Queries.GetNotifications;
using NotificationService.Application.Interfaces;
using Shared.Domain.Common;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;
    private readonly INotificationRepository _repository;

    public NotificationsController(
        IMediator mediator,
        ITenantContext tenantContext,
        INotificationRepository repository)
    {
        _mediator      = mediator;
        _tenantContext = tenantContext;
        _repository    = repository;
    }

    // GET /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(
            new GetNotificationsQuery(
                userId,
                _tenantContext.TenantId,
                page,
                pageSize),
            ct);

        return Ok(result.Value);
    }

    // GET /api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(
        CancellationToken ct)
    {
        var userId = GetUserId();

        var count = await _repository.GetUnreadCountAsync(
            userId, _tenantContext.TenantId, ct);

        return Ok(new { unreadCount = count });
    }

    // PUT /api/notifications/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken ct)
    {
        var notification = await _repository.GetByIdAsync(
            id, _tenantContext.TenantId, ct);

        if (notification is null)
            return NotFound();

        notification.MarkAsRead();
        await _repository.UpdateAsync(notification, ct);

        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value
                 ?? User.FindFirst("user_id")?.Value;

        return Guid.TryParse(claim, out var id)
            ? id : Guid.Empty;
    }
}
