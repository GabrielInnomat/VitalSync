namespace BuildingBlocks.Domain;

/// <summary>
/// Represents the state of an entity that can be modified by applying domain events.
/// </summary>
/// <remarks>
/// The state is an immutable snapshot: applying a domain event yields a new instance rather than mutating the current
/// one, which is what allows an event-sourced aggregate to be rebuilt deterministically by replaying its history.
/// Implement it as a record so equality and non-destructive mutation come for free.
/// </remarks>
/// <typeparam name="TSelf">The type of the state.</typeparam>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
public interface IState<TSelf, out TKey>
    where TSelf : IState<TSelf, TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Gets the unique identifier of the entity associated with this state.
    /// </summary>
    TKey Id { get; }

    /// <summary>
    /// Applies the specified domain event to the state, producing a new state that reflects the resulting changes.
    /// </summary>
    /// <remarks>
    /// Implementations must not mutate the current instance; they return a new state so that replaying the same event
    /// sequence always reconstructs the same result.
    /// </remarks>
    /// <param name="domainEvent">The domain event to apply to the state.</param>
    /// <returns>A new state that reflects the changes caused by the domain event.</returns>
    TSelf Apply(IDomainEvent domainEvent);
}
