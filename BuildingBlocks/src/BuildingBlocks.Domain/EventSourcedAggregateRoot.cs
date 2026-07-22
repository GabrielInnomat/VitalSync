namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for aggregate roots that are modeled as a stream of domain events,
/// chosen when the history of what happened carries business value. State is
/// immutable and derived by applying events. This is purely a domain-modeling
/// decision: the domain has no knowledge of how or where the aggregate is stored.
/// The event-sourcing surface (<see cref="IEventSourcedAggregateRoot{TKey}"/>) is
/// exposed only through explicit interface implementation so it does not appear
/// on a concrete aggregate's public API; infrastructure reaches it by casting.
/// </summary>
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

    /// <summary>
    /// Raises a new domain event. Invariants must be checked by the caller before
    /// this is invoked so a rule violation can never leave the aggregate in a
    /// half-mutated state. The event's <see cref="IDomainEvent.OccurredAt"/> is
    /// stamped here if it has not been set, keeping event records free of an
    /// ambient clock dependency.
    /// </summary>
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
