using MediatR;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using Shared.Domain.Common;

namespace NotificationService.Application.Commands.CreateNotification;

public sealed class CreateNotificationCommandHandler
    : IRequestHandler<CreateNotificationCommand, Result<Guid>>
{
    private readonly INotificationRepository _repository;

    public CreateNotificationCommandHandler(
        INotificationRepository repository)
        => _repository = repository;

    public async Task<Result<Guid>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            request.TenantId,
            request.UserId,
            request.Title,
            request.Message,
            request.Type,
            request.ReferenceId,
            request.ReferenceType);

        await _repository.AddAsync(notification, cancellationToken);

        return Result.Success(notification.Id);
    }
}
