namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a state of an entity that can be modified by applying domain events.
/// </summary>
/// <typeparam name="TSelf">The type of the state.</typeparam>
/// <typeparam name="TKey">The type of the entity key.</typeparam>
public interface IState<TSelf, out TKey>
    where TSelf : IState<TSelf, TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Gets the unique identifier of the entity associated with this state.
    /// </summary>
    TKey Id { get; }

    /// <summary>
    /// Applies the specified domain event to the state, resulting in a new state that reflects the changes caused by the event.
    /// </summary>
    /// <param name="domainEvent">The domain event to apply to the state.</param>
    /// <returns>A new state that reflects the changes caused by the domain event.</returns>
    TSelf Apply(IDomainEvent domainEvent);
}
