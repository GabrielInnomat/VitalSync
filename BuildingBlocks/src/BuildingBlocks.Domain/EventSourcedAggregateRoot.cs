namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an abstract base class for event-sourced aggregate roots in the domain model.
/// An aggregate root is a central entity that encapsulates a cluster of related entities and enforces consistency rules within the aggregate.
/// This class provides a foundation for implementing event sourcing, where state changes are captured as a sequence of domain events.
/// It manages the aggregate's state, identity, versioning, and domain events, allowing for the reconstruction of the aggregate's state from its event history.
/// </summary>
/// <typeparam name="TKey">The type of the aggregate root's identifier.</typeparam>
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
    /// Gets the current state of the aggregate root. The state is represented by an instance of <typeparamref name="TState"/>, which encapsulates the aggregate's data and behavior. The state can be modified by applying domain events, and it is used to enforce business rules and maintain consistency within the aggregate.
    /// </summary>
    public TState State { get; private set; } = initialState;

    /// <summary>
    /// Gets the unique identifier of the aggregate root. The identifier is of type <typeparamref name="TKey"/> and is used to uniquely identify the aggregate within the domain. It is derived from the current state of the aggregate and is expected to be set to a non-empty value by the applied events.
    /// </summary>
    public TKey Id => State.Id;

    /// <summary>
    /// Gets a read-only collection of domain events that have been raised by the aggregate root. These events represent significant occurrences within the aggregate and can be used to reconstruct its state or communicate changes to other parts of the system. The collection is read-only to prevent external modification, ensuring that the integrity of the aggregate's event history is maintained.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Gets the current version of the aggregate root. The version is a long integer that represents the number of events that have been applied to the aggregate. It is used for optimistic concurrency control, allowing the system to detect and handle conflicts when multiple processes attempt to modify the same aggregate concurrently.
    /// </summary>
    long IEventSourcedAggregateRoot<TKey>.Version => _version;

    /// <summary>
    /// Loads the aggregate root's state from a history of domain events. This method applies each event in the provided history to the aggregate's state, reconstructing its current state based on the sequence of events. It is intended for use during rehydration of the aggregate from an event store or other persistent storage. The method ensures that no uncommitted events have been raised before applying the history, throwing an exception if this condition is violated.
    /// </summary>
    /// <param name="history">The sequence of domain events to apply to the aggregate.</param>
    /// <exception cref="InvalidOperationException">Thrown if the aggregate has already raised uncommitted events.</exception>
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
    /// Raises a domain event and applies it to the aggregate's state. This method stamps the event with the current timestamp using the provided clock, applies the event to the aggregate's state, ensures that the aggregate's identity is valid, increments the version, and adds the stamped event to the collection of raised domain events. It is intended for use when a significant occurrence within the aggregate needs to be communicated to other parts of the system or persisted for future reconstruction of the aggregate's state.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise and apply to the aggregate's state.</param>
    /// <param name="clock">The clock used to stamp the event with the current timestamp.</param>
    /// <exception cref="ArgumentNullException">Thrown if the domain event or clock is null.</exception>
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

    /// <summary>
    /// Clears the collection of raised domain events. This method is part of the <see cref="IDomainEventsManager"/> interface and is intended to be called after the raised events have been persisted or published, allowing the aggregate to reset its event history and prepare for future changes. It ensures that the aggregate's state remains consistent and that no uncommitted events are retained after they have been handled.
    /// </summary>
    void IDomainEventsManager.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Determines whether the current aggregate root is equal to another aggregate root of the same type. Two aggregate roots are considered equal if they are of the same type and have the same identifier. This method is part of the <see cref="IEquatable{T}"/> interface and provides a strongly-typed equality comparison for aggregate roots, allowing for efficient comparisons in collections and other scenarios where equality checks are required.
    /// </summary>
    /// <param name="other">The other aggregate root to compare with the current aggregate root.</param>
    /// <returns>true if the current aggregate root is equal to the other aggregate root; otherwise, false.</returns>
    public bool Equals(EventSourcedAggregateRoot<TKey, TState>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether the current aggregate root is equal to another object. This method overrides the default implementation of <see cref="object.Equals(object)"/> and provides a type-safe equality comparison for aggregate roots. It checks whether the other object is an instance of <see cref="EventSourcedAggregateRoot{TKey, TState}"/> and delegates the equality check to the strongly-typed <see cref="Equals(EventSourcedAggregateRoot{TKey, TState})"/> method.
    /// </summary>
    /// <param name="obj">The object to compare with the current aggregate root.</param>
    /// <returns>true if the current aggregate root is equal to the other object; otherwise, false.</returns>
    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as EventSourcedAggregateRoot<TKey, TState>);
    }

    /// <summary>
    /// Returns a hash code for the current aggregate root. This method overrides the default implementation of <see cref="object.GetHashCode()"/> and provides a hash code that is based on the aggregate root's type and identifier. The hash code is used in hash-based collections, such as dictionaries and hash sets, to efficiently store and retrieve aggregate roots based on their identity.
    /// </summary>
    /// <returns>A hash code for the current aggregate root.</returns>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Determines whether two aggregate roots are equal. This operator overload provides a convenient way to compare two instances of <see cref="EventSourcedAggregateRoot{TKey, TState}"/> for equality. It checks for reference equality first, and if the references are not equal, it delegates the equality check to the <see cref="Equals(EventSourcedAggregateRoot{TKey, TState})"/> method.
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
    /// Determines whether two aggregate roots are not equal. This operator overload provides a convenient way to compare two instances of <see cref="EventSourcedAggregateRoot{TKey, TState}"/> for inequality. It checks for reference equality first, and if the references are not equal, it delegates the inequality check to the negation of the <see cref="Equals(EventSourcedAggregateRoot{TKey, TState})"/> method.
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
