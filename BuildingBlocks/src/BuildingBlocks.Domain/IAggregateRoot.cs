namespace BuildingBlocks.Domain;

public interface IAggregateRoot<out TKey> : IEntity<TKey>, IHasDomainEvents
    where TKey : struct, IEntityKey;
