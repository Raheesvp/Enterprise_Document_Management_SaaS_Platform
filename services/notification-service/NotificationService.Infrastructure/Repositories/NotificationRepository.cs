using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Repositories;

public sealed class NotificationRepository
    : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
        => _context = context;

    public async Task AddAsync(
        Notification notification,
        CancellationToken ct = default)
    {
        await _context.Notifications.AddAsync(notification, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Notification?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == id && n.TenantId == tenantId, ct);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId
                     && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        return await _context.Notifications
            .CountAsync(
                n => n.UserId == userId
                  && n.TenantId == tenantId
                  && n.Status ==
                     NotificationService.Domain.Enums
                         .NotificationStatus.Unread,
                ct);
    }

    public async Task UpdateAsync(
        Notification notification,
        CancellationToken ct = default)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync(ct);
    }
}
