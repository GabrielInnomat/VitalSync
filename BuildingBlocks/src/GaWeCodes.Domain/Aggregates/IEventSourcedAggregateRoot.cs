using GaWeCodes.Domain.Entities;
using GaWeCodes.Domain.Events;

namespace GaWeCodes.Domain.Aggregates;

public interface IEventSourcedAggregateRoot<TKey> : IAggregateRoot<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    void LoadFromHistory(IEnumerable<IDomainEvent> history);
}
