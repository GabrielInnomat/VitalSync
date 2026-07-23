namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for domain entities that are compared by identity rather than by attribute values.
/// </summary>
/// <remarks>
/// Deriving from this class gives an entity a validated identity and correct identity-based equality (including the
/// matching <c>==</c>/<c>!=</c> operators and <see cref="GetHashCode"/>) without repeating that boilerplate per type.
/// Two entities are considered equal when they are the same concrete type and share the same <see cref="Id"/>.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
public abstract class Entity<TKey> : IEntity<TKey>, IEquatable<Entity<TKey>>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TKey}"/> class with the specified unique identifier.
    /// </summary>
    /// <remarks>
    /// The identity is validated eagerly so an entity can never exist in a state without a usable identifier.
    /// </remarks>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <exception cref="DomainValidationException">Thrown when <paramref name="id"/> is empty.</exception>
    protected Entity(TKey id)
    {
        if (id.IsEmpty)
        {
            throw new DomainValidationException("The id of an entity cannot be empty.");
        }

        Id = id;
    }

    /// <inheritdoc/>
    public TKey Id { get; }

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity.
    /// </summary>
    /// <remarks>
    /// Two entities are considered equal when they are the same concrete type and share the same <see cref="Id"/>.
    /// </remarks>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns><c>true</c> if the specified entity is equal to the current entity; otherwise, <c>false</c>.</returns>
    public bool Equals(Entity<TKey>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns><c>true</c> if the specified object is equal to the current entity; otherwise, <c>false</c>.</returns>
    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as Entity<TKey>);
    }

    /// <summary>
    /// Returns a hash code for the current entity.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><c>true</c> if the two entities are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
    {
        return ReferenceEquals(left, right) || (left is not null && right is not null && left.Equals(right));
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><c>true</c> if the two entities are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
    {
        return !(left == right);
    }
}
