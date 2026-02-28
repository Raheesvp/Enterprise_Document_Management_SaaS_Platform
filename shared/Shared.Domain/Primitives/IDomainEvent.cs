using MediatR;

namespace Shared.Domain.Events;

public interface IDomainEvent : INotification
{
    Guid EventId {get;}
    DateTime OccuredOn {get;}
}