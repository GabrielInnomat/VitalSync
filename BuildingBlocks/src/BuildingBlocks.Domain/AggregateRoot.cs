namespace BuildingBlocks.Domain;

/// <summary>
/// Represents the base class for aggregate roots in the domain model.
/// This class is used for aggregates that are not event sourced.
/// </summary>
/// <typeparam name="TKey">The type of the aggregate root's identifier.</typeparam>
public abstract class AggregateRoot<TKey>
    : IAggregateRoot<TKey>, IDomainEventsManager, IEquatable<AggregateRoot<TKey>>
    where TKey : struct, IEntityKey
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TKey}"/> class with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the aggregate root.</param>
    protected AggregateRoot(TKey id)
    {
        EnsureValidIdentity(id);
        Id = id;
    }

    /// <summary>
    /// Gets the identifier of the aggregate root.
    /// </summary>
    public TKey Id { get; }

    /// <summary>
    /// Gets the collection of domain events associated with the aggregate root.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate root's collection of domain events.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="domainEvent"/> is null.</exception>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the aggregate root's collection of domain events.
    /// </summary>
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
    /// Determines whether the specified <see cref="AggregateRoot{TKey}"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="AggregateRoot{TKey}"/> to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified <see cref="AggregateRoot{TKey}"/> is equal to the current instance; otherwise, <c>false</c>.</returns>
    public bool Equals(AggregateRoot<TKey>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as AggregateRoot<TKey>);
    }

    /// <summary>
    /// Returns a hash code for the current instance.
    /// </summary>
    /// <returns>A hash code for the current instance.</returns>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Determines whether two <see cref="AggregateRoot{TKey}"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="AggregateRoot{TKey}"/> to compare.</param>
    /// <param name="right">The second <see cref="AggregateRoot{TKey}"/> to compare.</param>
    /// <returns><c>true</c> if the two <see cref="AggregateRoot{TKey}"/> instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(AggregateRoot<TKey>? left, AggregateRoot<TKey>? right)
    {
        return ReferenceEquals(left, right) || (left is not null && right is not null && left.Equals(right));
    }

    /// <summary>
    /// Determines whether two <see cref="AggregateRoot{TKey}"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="AggregateRoot{TKey}"/> to compare.</param>
    /// <param name="right">The second <see cref="AggregateRoot{TKey}"/> to compare.</param>
    /// <returns><c>true</c> if the two <see cref="AggregateRoot{TKey}"/> instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(AggregateRoot<TKey>? left, AggregateRoot<TKey>? right)
    {
        return !(left == right);
    }
}
