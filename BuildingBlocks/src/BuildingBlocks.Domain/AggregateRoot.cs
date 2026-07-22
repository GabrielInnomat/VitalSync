namespace BuildingBlocks.Domain;

public abstract class AggregateRoot<TKey>
    : IAggregateRoot<TKey>, IDomainEventsManager, IEquatable<AggregateRoot<TKey>>
    where TKey : struct, IEntityKey
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TKey id)
    {
        EnsureValidIdentity(id);
        Id = id;
    }

    public TKey Id { get; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected sealed void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    void IDomainEventsManager.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private static void EnsureValidIdentity(TKey id)
    {
        if (id is IEntityKey<Guid> guidKey && guidKey.Value == Guid.Empty)
            throw new DomainValidationException(
                "The id of an aggregate cannot be an empty GUID.");
    }

    public bool Equals(AggregateRoot<TKey>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as AggregateRoot<TKey>);
    }

    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(AggregateRoot<TKey>? left, AggregateRoot<TKey>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(AggregateRoot<TKey>? left, AggregateRoot<TKey>? right)
    {
        return !(left == right);
    }
}
