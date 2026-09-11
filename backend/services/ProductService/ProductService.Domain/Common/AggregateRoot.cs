namespace ProductService.Domain.Common;

public abstract class AggregateRoot
{
    private readonly List<object> _domainEvents = new();

    public Guid Id { get; protected set; }

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(object domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
