namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a base class for entities in the domain model. An entity is an object that has a unique identity and is defined by its identity rather than its attributes. This class provides a common implementation for equality comparison based on the entity's unique identifier.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
public abstract class Entity<TKey> : IEntity<TKey>, IEquatable<Entity<TKey>>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TKey}"/> class with the specified unique identifier. The constructor ensures that the provided identifier is not empty, throwing a <see cref="DomainValidationException"/> if it is. This enforces the rule that every entity must have a valid, non-empty identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <exception cref="DomainValidationException">Thrown when the provided identifier is empty.</exception>
    protected Entity(TKey id)
    {
        if (id.IsEmpty)
        {
            throw new DomainValidationException("The id of an entity cannot be empty.");
        }

        Id = id;
    }

    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public TKey Id { get; }

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity based on their unique identifiers. Two entities are considered equal if they are of the same type and have the same identifier.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns><c>true</c> if the specified entity is equal to the current entity; otherwise, <c>false</c>.</returns>
    public bool Equals(Entity<TKey>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity. This method overrides the default implementation of <see cref="object.Equals(object?)"/> and provides a type-safe comparison for entities.
    /// </summary>
    /// <param name="obj">The object to compare the instance with.</param>
    /// <returns><c>true</c> if the specified object is equal to the current entity; otherwise, <c>false</c>.</returns>
    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as Entity<TKey>);
    }

    /// <summary>
    /// Returns a hash code for the entity based on its type and unique identifier. This method is used in hashing algorithms and data structures such as a hash table.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }
    /// <summary>
    /// Determines whether two entities are equal based on their unique identifiers. Two entities are considered equal if they are of the same type and have the same identifier.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><c>true</c> if the objects are equal else <c>false</c></returns>
    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
    {
        return ReferenceEquals(left, right) || (left is not null && right is not null && left.Equals(right));
    }

    /// <summary>
    /// Determines whether two entities are not equal based on their unique identifiers. Two entities are considered not equal if they are of different types or have different identifiers.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><c>true</c> if the objects are not equal else <c>false</c></returns>
    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
    {
        return !(left == right);
    }
}
