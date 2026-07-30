namespace BuildingBlocks.Domain;

/// <summary>
/// Base class providing identity-based equality for domain objects that are identified by an identity key.
/// </summary>
/// <remarks>
/// This class exists so the identity-equality contract (see ADR-0008) is implemented exactly once and shared by
/// <see cref="Entity{TKey}"/> and <see cref="AggregateRoot{TKey, TState}"/> instead of being duplicated per base
/// class. It cannot be derived from directly outside this assembly; derive from one of those two bases instead.
/// Two instances are considered equal when they are the same concrete type and share the same <see cref="Id"/>.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
public abstract class EntityBase<TKey> : IEntity<TKey>, IEquatable<EntityBase<TKey>>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityBase{TKey}"/> class.
    /// </summary>
    /// <remarks>
    /// The constructor is <see langword="private protected"/> so that only the in-package bases
    /// (<see cref="Entity{TKey}"/> and <see cref="AggregateRoot{TKey, TState}"/>) can extend this class; domain code
    /// derives from those instead.
    /// </remarks>
    private protected EntityBase()
    {
    }

    /// <inheritdoc/>
    public abstract TKey Id { get; }

    /// <summary>
    /// Determines whether the specified domain object is equal to the current domain object.
    /// </summary>
    /// <remarks>
    /// Two instances are considered equal when they are the same concrete type and share the same <see cref="Id"/>.
    /// </remarks>
    /// <param name="other">The domain object to compare with the current domain object.</param>
    /// <returns><c>true</c> if the specified domain object is equal to the current domain object; otherwise, <c>false</c>.</returns>
    public bool Equals(EntityBase<TKey>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current domain object.
    /// </summary>
    /// <param name="obj">The object to compare with the current domain object.</param>
    /// <returns><c>true</c> if the specified object is equal to the current domain object; otherwise, <c>false</c>.</returns>
    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as EntityBase<TKey>);
    }

    /// <summary>
    /// Returns a hash code for the current domain object.
    /// </summary>
    /// <returns>A hash code for the current domain object.</returns>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Determines whether two domain objects are equal.
    /// </summary>
    /// <param name="left">The first domain object to compare.</param>
    /// <param name="right">The second domain object to compare.</param>
    /// <returns><c>true</c> if the two domain objects are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(EntityBase<TKey>? left, EntityBase<TKey>? right)
    {
        return ReferenceEquals(left, right) || (left is not null && right is not null && left.Equals(right));
    }

    /// <summary>
    /// Determines whether two domain objects are not equal.
    /// </summary>
    /// <param name="left">The first domain object to compare.</param>
    /// <param name="right">The second domain object to compare.</param>
    /// <returns><c>true</c> if the two domain objects are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(EntityBase<TKey>? left, EntityBase<TKey>? right)
    {
        return !(left == right);
    }
}
