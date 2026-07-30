namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an aggregate root in the domain-driven design (DDD) context.
/// </summary>
/// <remarks>
/// An aggregate root is the main entity that controls access to a group of related entities (the aggregate) and
/// ensures the consistency of changes within it. This is the common contract for all aggregates; event-sourced
/// aggregates additionally expose <see cref="IEventSourcedAggregateRoot{TKey}"/>, which extends this interface with
/// functionality for reconstructing state from domain events.
/// </remarks>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
public interface IAggregateRoot<out TKey> : IEntity<TKey>, IHasDomainEvents
    where TKey : struct, IEntityKey;
