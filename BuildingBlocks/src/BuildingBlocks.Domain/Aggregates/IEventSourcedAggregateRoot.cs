using BuildingBlocks.Domain.Entities;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Domain.Aggregates;

public interface IEventSourcedAggregateRoot<TKey> : IAggregateRoot<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    void LoadFromHistory(IEnumerable<IDomainEvent> history);
}
