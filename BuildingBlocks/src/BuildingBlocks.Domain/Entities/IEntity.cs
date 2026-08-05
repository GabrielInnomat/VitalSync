namespace BuildingBlocks.Domain.Entities;

public interface IEntity<out TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    TKey Id { get; }
}
