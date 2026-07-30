namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for domain entities that are compared by identity rather than by attribute values.
/// </summary>
/// <remarks>
/// Deriving from this class gives an entity a validated identity and correct identity-based equality (including the
/// matching <c>==</c>/<c>!=</c> operators and <c>GetHashCode</c>, inherited from <see cref="EntityBase{TKey}"/>)
/// without repeating that boilerplate per type. Two entities are considered equal when they are the same concrete
/// type and share the same <see cref="Id"/>.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
public abstract class Entity<TKey> : EntityBase<TKey>
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
    public sealed override TKey Id { get; }
}
