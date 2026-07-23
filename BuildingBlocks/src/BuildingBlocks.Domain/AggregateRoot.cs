namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for aggregate roots that are not event-sourced.
/// </summary>
/// <remarks>
/// This base collects the domain events raised while handling a command and exposes them for dispatch after the
/// aggregate is persisted, while enforcing a valid identity and identity-based equality. Use it for aggregates whose
/// current state is stored directly; for aggregates rebuilt from their event history use
/// <see cref="EventSourcedAggregateRoot{TKey, TState}"/> instead. Two aggregate roots are considered equal when they
/// are the same concrete type and share the same <see cref="Id"/>.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
public abstract class AggregateRoot<TKey>
    : IAggregateRoot<TKey>, IDomainEventsManager, IEquatable<AggregateRoot<TKey>>
    where TKey : struct, IEntityKey
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TKey}"/> class with the specified unique identifier.
    /// </summary>
    /// <remarks>
    /// The identity is validated eagerly so an aggregate can never exist in a state without a usable identifier.
    /// </remarks>
    /// <param name="id">The unique identifier of the aggregate root.</param>
    /// <exception cref="DomainValidationException">Thrown when <paramref name="id"/> is empty.</exception>
    protected AggregateRoot(TKey id)
    {
        EnsureValidIdentity(id);
        Id = id;
    }

    /// <inheritdoc/>
    public TKey Id { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate root's collection of domain events.
    /// </summary>
    /// <remarks>
    /// Call this from within the aggregate whenever a state change occurs that other parts of the system may need to
    /// react to; the event is recorded now and dispatched after the aggregate has been persisted.
    /// </remarks>
    /// <param name="domainEvent">The domain event to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.</exception>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc/>
    void IDomainEventsManager.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private static void EnsureValidIdentity(TKey id)
    {
        if (id.IsEmpty)
        {
            throw new DomainValidationException(
                "The id of an aggregate cannot be empty.");
        }
    }

    /// <summary>
    /// Determines whether the specified aggregate root is equal to the current aggregate root.
    /// </summary>
    /// <remarks>
    /// Two aggregate roots are considered equal when they are the same concrete type and share the same <see cref="Id"/>.
    /// </remarks>
    /// <param name="other">The aggregate root to compare with the current aggregate root.</param>
    /// <returns><c>true</c> if the specified aggregate root is equal to the current aggregate root; otherwise, <c>false</c>.</returns>
    public bool Equals(AggregateRoot<TKey>? other)
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
        return Equals(obj as AggregateRoot<TKey>);
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
    public static bool operator ==(AggregateRoot<TKey>? left, AggregateRoot<TKey>? right)
    {
        return ReferenceEquals(left, right) || (left is not null && right is not null && left.Equals(right));
    }

    /// <summary>
    /// Determines whether two aggregate roots are not equal.
    /// </summary>
    /// <param name="left">The first aggregate root to compare.</param>
    /// <param name="right">The second aggregate root to compare.</param>
    /// <returns><c>true</c> if the two aggregate roots are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(AggregateRoot<TKey>? left, AggregateRoot<TKey>? right)
    {
        return !(left == right);
    }
}
