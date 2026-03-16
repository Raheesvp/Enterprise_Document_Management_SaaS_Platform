using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(
        Notification notification,
        CancellationToken ct = default);

    Task<Notification?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default);

    Task UpdateAsync(
        Notification notification,
        CancellationToken ct = default);
}
