namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an entity that is uniquely identified by an identity key.
/// </summary>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
public interface IEntity<out TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    TKey Id { get; }
}
