namespace BuildingBlocks.Domain;

public interface IEntityKey;

public interface IEntityKey<out TValue> : IEntityKey
    where TValue : notnull
{
    TValue Value { get; }
}
