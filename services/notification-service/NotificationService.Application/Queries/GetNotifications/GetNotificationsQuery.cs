using MediatR;
using NotificationService.Application.DTOs;
using Shared.Domain.Common;

namespace NotificationService.Application.Queries.GetNotifications;

public record GetNotificationsQuery(
    Guid UserId,
    Guid TenantId,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<List<NotificationDto>>>;
