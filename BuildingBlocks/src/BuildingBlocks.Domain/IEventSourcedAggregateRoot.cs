namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an aggregate root whose state is derived from a sequence of domain events rather than being stored directly.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IAggregateRoot{TKey}"/> and adds functionality for reconstructing the aggregate's
/// state from a history of domain events.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
public interface IEventSourcedAggregateRoot<out TKey> : IAggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Gets the version of the aggregate root, representing the number of events that have been applied to it.
    /// </summary>
    /// <remarks>
    /// This is used for optimistic concurrency control and to ensure that the aggregate's state is consistent.
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
