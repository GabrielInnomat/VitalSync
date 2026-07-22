namespace BuildingBlocks.Domain;

public abstract class EventSourcedAggregateRoot<TKey, TState>
    : IAggregateRoot<TKey>, IDomainEventsManager,
      IEquatable<EventSourcedAggregateRoot<TKey, TState>>, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private long _version;

    protected EventSourcedAggregateRoot(TState initialState)
    {
        State = initialState;
    }

    public TState State { get; private set; }

    public TKey Id => State.Id;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    long IEventSourcedAggregateRoot<TKey>.Version => _version;

    void IEventSourcedAggregateRoot<TKey>.LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var domainEvent in history)
        {
            State = State.Apply(domainEvent);
            _version++;
        }

        EnsureValidIdentity();
    }

    protected void RaiseEvent(IDomainEvent domainEvent, IClock clock)
    {
        var stamped = Stamp(domainEvent, clock);
        State = State.Apply(stamped);
        EnsureValidIdentity();
        _version++;
        _domainEvents.Add(stamped);
    }

    private static IDomainEvent Stamp(IDomainEvent domainEvent, IClock clock)
    {
        return domainEvent is DomainEvent { OccurredAt.Ticks: 0 } record
            ? record with { OccurredAt = clock.Now }
            : domainEvent;
    }

    private void EnsureValidIdentity()
    {
        if (State.Id is IEntityKey<Guid> guidKey && guidKey.Value == Guid.Empty)
            throw new DomainValidationException(
                "The aggregate's identity must be set to a non-empty value by the applied event.");
    }

    void IDomainEventsManager.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public bool Equals(EventSourcedAggregateRoot<TKey, TState>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as EventSourcedAggregateRoot<TKey, TState>);
    }

    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(
        EventSourcedAggregateRoot<TKey, TState>? left,
        EventSourcedAggregateRoot<TKey, TState>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(
        EventSourcedAggregateRoot<TKey, TState>? left,
        EventSourcedAggregateRoot<TKey, TState>? right)
    {
        return !(left == right);
    }
}
