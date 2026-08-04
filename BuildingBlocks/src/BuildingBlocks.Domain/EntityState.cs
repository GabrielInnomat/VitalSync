namespace BuildingBlocks.Domain;

public abstract record EntityState<TSelf, TKey>
    where TSelf : EntityState<TSelf, TKey>
    where TKey : struct, IEntityKey
{
    public abstract TKey Id { get; init; }

    public abstract TSelf Apply(IDomainEvent domainEvent);
}
