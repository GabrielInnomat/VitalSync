namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for event-sourced aggregate roots, whose state is derived from a sequence of domain events.
/// </summary>
/// <remarks>
/// This class provides the machinery for event sourcing: it holds the current <see cref="State"/>, tracks a version for
/// optimistic concurrency, records raised events for dispatch, and rebuilds state by replaying history. Derive from it
/// when an aggregate's source of truth is its event stream rather than a directly stored snapshot; otherwise use
/// <see cref="AggregateRoot{TKey}"/>. Two aggregate roots are considered equal when they are the same concrete type and
/// share the same <see cref="Id"/>.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
/// <typeparam name="TState">The type of the aggregate root's state.</typeparam>
/// <param name="initialState">The initial state of the aggregate root.</param>
public abstract class EventSourcedAggregateRoot<TKey, TState>(TState initialState)
    : IAggregateRoot<TKey>, IDomainEventsManager,
      IEquatable<EventSourcedAggregateRoot<TKey, TState>>, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private long _version;

    /// <summary>
    /// Gets the current state of the aggregate root.
    /// </summary>
    /// <remarks>
    /// The state, represented by an instance of <typeparamref name="TState"/>, encapsulates the aggregate's data and is
    /// replaced by a new instance whenever a domain event is applied.
    /// </remarks>
    public TState State { get; private set; } = initialState;

    /// <summary>
    /// Gets the unique identifier of the aggregate root.
    /// </summary>
    /// <remarks>
    /// The identifier, of type <typeparamref name="TKey"/>, is derived from the current <see cref="State"/>.
    /// </remarks>
    public TKey Id => State.Id;

    /// <inheritdoc/>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc/>
    long IEventSourcedAggregateRoot<TKey>.Version => _version;

    /// <inheritdoc/>
    void IEventSourcedAggregateRoot<TKey>.LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        if (_domainEvents.Count > 0)
        {
            throw new InvalidOperationException(
                "LoadFromHistory cannot be called after events have been raised on the aggregate.");
        }

        foreach (var domainEvent in history)
        {
            State = State.Apply(domainEvent);
            _version++;
        }

        EnsureValidIdentity();
    }

    /// <summary>
    /// Raises a domain event and applies it to the aggregate's state.
    /// </summary>
    /// <remarks>
    /// This method stamps the event with the current timestamp using the provided clock, applies the event to the
    /// aggregate's state, increments the version, and records the event as uncommitted.
    /// </remarks>
    /// <param name="domainEvent">The domain event to raise and apply to the aggregate's state.</param>
    /// <param name="clock">The clock used to stamp the event with the current timestamp.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> or <paramref name="clock"/> is <see langword="null"/>.</exception>
    protected void RaiseEvent(IDomainEvent domainEvent, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(clock);

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
        if (State.Id.IsEmpty)
        {
            throw new DomainValidationException(
                "The aggregate's identity must be set to a non-empty value by the applied event.");
        }
    }

    /// <inheritdoc/>
    void IDomainEventsManager.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Determines whether the specified aggregate root is equal to the current aggregate root.
    /// </summary>
    /// <remarks>
    /// Two aggregate roots are considered equal when they are the same concrete type and share the same <see cref="Id"/>.
    /// </remarks>
    /// <param name="other">The aggregate root to compare with the current aggregate root.</param>
    /// <returns><c>true</c> if the specified aggregate root is equal to the current aggregate root; otherwise, <c>false</c>.</returns>
    public bool Equals(EventSourcedAggregateRoot<TKey, TState>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current aggregate root.
    /// </summary>
    /// <param name="obj">The object to compare with the current aggregate root.</param>
    /// <returns><c>true</c> if the specified object is equal to the current aggregate root; otherwise, <c>false</c>.</returns>
    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as EventSourcedAggregateRoot<TKey, TState>);
    }

    /// <summary>
    /// Returns a hash code for the current aggregate root.
    /// </summary>
    /// <returns>A hash code for the current aggregate root.</returns>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Determines whether two aggregate roots are equal.
    /// </summary>
    /// <param name="left">The first aggregate root to compare.</param>
    /// <param name="right">The second aggregate root to compare.</param>
    /// <returns><c>true</c> if the two aggregate roots are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(
        EventSourcedAggregateRoot<TKey, TState>? left,
        EventSourcedAggregateRoot<TKey, TState>? right)
    {
        return ReferenceEquals(left, right) || (left is not null && right is not null && left.Equals(right));
    }

    /// <summary>
    /// Determines whether two aggregate roots are not equal.
    /// </summary>
    /// <param name="left">The first aggregate root to compare.</param>
    /// <param name="right">The second aggregate root to compare.</param>
    /// <returns><c>true</c> if the two aggregate roots are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(
        EventSourcedAggregateRoot<TKey, TState>? left,
        EventSourcedAggregateRoot<TKey, TState>? right)
    {
        return !(left == right);
    }
}
