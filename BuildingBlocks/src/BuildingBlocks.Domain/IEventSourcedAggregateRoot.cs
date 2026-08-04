namespace BuildingBlocks.Domain;

public interface IEventSourcedAggregateRoot<out TKey> : IAggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    void LoadFromHistory(IEnumerable<IDomainEvent> history);
}
