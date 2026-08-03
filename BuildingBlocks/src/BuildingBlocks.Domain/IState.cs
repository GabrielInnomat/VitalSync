namespace BuildingBlocks.Domain;

public interface IState<TSelf, out TKey>
    where TSelf : IState<TSelf, TKey>
    where TKey : struct, IEntityKey
{
    TKey Id { get; }

    TSelf Apply(IDomainEvent domainEvent);
}
