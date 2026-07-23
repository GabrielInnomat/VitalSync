namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an aggregate root in the domain-driven design (DDD) context. An aggregate root is the main entity that
/// controls access to a group of related entities (the aggregate) and ensures the consistency of changes within it.
/// </summary>
/// <remarks>
/// This is the base interface, used for aggregates that do not support event sourcing. To use event sourcing, use the
/// <see cref="IEventSourcedAggregateRoot{TKey}"/> interface instead, which extends this interface and adds additional
/// functionality for handling domain events and event sourcing.
/// </remarks>
/// <typeparam name="TKey">The type of the key.</typeparam>
public interface IAggregateRoot<out TKey> : IEntity<TKey>, IHasDomainEvents
    where TKey : struct, IEntityKey;
