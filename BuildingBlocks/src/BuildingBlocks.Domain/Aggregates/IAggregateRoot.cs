using BuildingBlocks.Domain.Entities;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Domain.Aggregates;

public interface IAggregateRoot<out TKey> : IEntity<TKey>, IHasDomainEvents
    where TKey : struct, IEntityKey, IEquatable<TKey>;
