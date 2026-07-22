namespace BuildingBlocks.Domain;

public interface IEntity<out TKey>
    where TKey : struct, IEntityKey
{
    TKey Id { get; }
}
