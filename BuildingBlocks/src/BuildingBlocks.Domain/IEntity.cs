namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an entity with a unique identifier of type <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier for the entity.</typeparam>
public interface IEntity<out TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    TKey Id { get; }
}
