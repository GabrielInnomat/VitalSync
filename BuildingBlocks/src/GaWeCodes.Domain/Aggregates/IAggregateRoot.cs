using GaWeCodes.Domain.Entities;
using GaWeCodes.Domain.Events;

namespace GaWeCodes.Domain.Aggregates;

public interface IAggregateRoot<TKey> : IEntity<TKey>, IHasDomainEvents
    where TKey : struct, IEntityKey, IEquatable<TKey>;
