using BuildingBlocks.Domain.Entities;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Domain.Aggregates;

public interface IAggregateRoot<TKey> : IEntity<TKey>, IHasDomainEvents
    where TKey : struct, IEntityKey, IEquatable<TKey>;
