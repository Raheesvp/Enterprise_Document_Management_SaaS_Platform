
namespace Shared.Domain.Primitives;

using Shared.Domain.Events;

public abstract class BaseEntity<TId> where TId:notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected BaseEntity(TId id)
    {
        Id=id;
    }


    protected BaseEntity(){ }

    public TId Id {get; private set;} =default!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents()=>_domainEvents.Clear();


}