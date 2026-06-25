namespace BuildingBlocks.Domain;

public interface IState<out TKey>
    where TKey : struct, IEntityKey
{
    TKey Id { get; }

    IState<TKey> Apply(IDomainEvent domainEvent);
}