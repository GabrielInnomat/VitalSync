namespace BuildingBlocks.Domain;

/// <summary>
/// Implemented by aggregates whose behavior is modeled as a stream of domain
/// events because that history carries business value. This is a domain-modeling
/// choice, not a storage choice: the persistence layer detects this interface to
/// decide how to physically persist the aggregate, but the domain remains
/// entirely unaware of any storage strategy.
/// </summary>
public interface IEventSourcedAggregateRoot<out TKey> : IAggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    long Version { get; }

    void LoadFromHistory(IEnumerable<IDomainEvent> history);
}
