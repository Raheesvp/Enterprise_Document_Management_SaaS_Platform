using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using Shared.Domain.Common;

namespace NotificationService.Application.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery,
        Result<List<NotificationDto>>>
{
    private readonly INotificationRepository _repository;

    public GetNotificationsQueryHandler(
        INotificationRepository repository)
        => _repository = repository;

    public async Task<Result<List<NotificationDto>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var notifications = await _repository.GetByUserIdAsync(
            request.UserId,
            request.TenantId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = notifications.Select(n => new NotificationDto(
            n.Id,
            n.TenantId,
            n.UserId,
            n.Title,
            n.Message,
            n.Type.ToString(),
            n.Status.ToString(),
            n.ReferenceId,
            n.ReferenceType,
            n.CreatedAt,
            n.ReadAt)).ToList();

        return Result.Success(dtos);
    }
}
