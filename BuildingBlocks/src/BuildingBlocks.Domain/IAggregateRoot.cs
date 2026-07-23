namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an aggregate root in the domain-driven design (DDD) context. An aggregate root is the main entity that controls access to a group of related entities (the aggregate). It ensures the integrity and consistency of the aggregate by enforcing business rules and invariants.
/// This is the base interface. This means it is used for aggregates that do not support event sourcing.
/// If you want to use event sourcing, you should use the <see cref="IEventSourcedAggregateRoot{TKey}"/> interface instead.
/// The event sourcing aggregate root interface extends this interface and adds additional functionality for handling domain events and event sourcing.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
public interface IAggregateRoot<out TKey> : IEntity<TKey>, IHasDomainEvents
    where TKey : struct, IEntityKey;
