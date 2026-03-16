using MediatR;
using NotificationService.Domain.Enums;
using Shared.Domain.Common;

namespace NotificationService.Application.Commands.CreateNotification;

public record CreateNotificationCommand(
    Guid TenantId,
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type,
    Guid? ReferenceId = null,
    string? ReferenceType = null)
    : IRequest<Result<Guid>>;
