namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an aggregate root that is event-sourced, meaning its state is derived from a sequence of domain events
/// rather than being stored directly.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IAggregateRoot{TKey}"/> and adds functionality for loading the aggregate's state
/// from a history of domain events.
/// </remarks>
/// <typeparam name="TKey">The type of the key.</typeparam>
public interface IEventSourcedAggregateRoot<out TKey> : IAggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Gets the version of the aggregate root, which represents the number of events that have been applied to it.
    /// </summary>
    /// <remarks>
    /// This is useful for optimistic concurrency control and ensuring that the aggregate's state is consistent.
    /// </remarks>
    long Version { get; }

    /// <summary>
    /// Loads the aggregate's state from a history of domain events.
    /// </summary>
    /// <remarks>
    /// This method is typically called when reconstructing the aggregate from an event store or when replaying events
    /// to rebuild its state.
    /// </remarks>
    /// <param name="history">The sequence of domain events to apply to the aggregate.</param>
    void LoadFromHistory(IEnumerable<IDomainEvent> history);
}
