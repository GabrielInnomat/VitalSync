namespace BuildingBlocks.Domain;

public interface IEventSourcedAggregateRoot<out TKey> : IAggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    long Version { get; }

    void LoadFromHistory(IEnumerable<IDomainEvent> history);
}
