namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an entity that is uniquely identified by an identity key.
/// </summary>
/// <remarks>
/// Entities are compared by identity rather than by their attribute values, so this contract exposes only the
/// identity key that establishes that identity. Implement it on domain objects whose lifetime and equality are
/// defined by a stable identifier rather than by the data they currently hold.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
public interface IEntity<out TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    TKey Id { get; }
}
