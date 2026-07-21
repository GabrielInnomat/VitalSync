namespace BuildingBlocks.Domain;

public abstract class AggregateRoot<TKey, TState>(TState initialState)
    : IAggregateRoot<TKey>, IDomainEventsManager, IEquatable<AggregateRoot<TKey, TState>>, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : IState<TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private long _version;

    public TState State { get; private set; } = initialState;

    public TKey Id => State.Id;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Event-sourcing capability is exposed only through the explicit interface
    // implementation below, so it never appears on a concrete aggregate's public
    // surface. State-stored (EF Core) aggregates use the same base class without
    // seeing these members; event-sourcing infrastructure reaches them by casting
    // to IEventSourcedAggregateRoot<TKey>.
    long IEventSourcedAggregateRoot<TKey>.Version => _version;

    void IEventSourcedAggregateRoot<TKey>.LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        if (_version > 0)
            throw new DomainValidationException(
                "Cannot rehydrate an aggregate that already has state; " +
                "LoadFromHistory must be called on a fresh instance.");

        foreach (var domainEvent in history)
        {
            State = ApplyToState(domainEvent);
            EnsureValidIdentity();
            _version++;
        }
    }

    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        State = ApplyToState(domainEvent);
        EnsureValidIdentity();
        _version++;
        _domainEvents.Add(domainEvent);
    }

    private TState ApplyToState(IDomainEvent domainEvent)
    {
        return (TState)State.Apply(domainEvent);
    }

    private void EnsureValidIdentity()
    {
        if (State.Id.Value == Guid.Empty)
            throw new DomainValidationException(
                "The aggregate's identity must be set to a non-empty value by the applied event.");
    }

    void IDomainEventsManager.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public bool Equals(AggregateRoot<TKey, TState>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as AggregateRoot<TKey, TState>);
    }

    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(AggregateRoot<TKey, TState>? left, AggregateRoot<TKey, TState>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(AggregateRoot<TKey, TState>? left, AggregateRoot<TKey, TState>? right)
    {
        return !(left == right);
    }
}
