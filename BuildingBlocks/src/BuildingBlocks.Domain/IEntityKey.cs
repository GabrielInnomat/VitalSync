namespace BuildingBlocks.Domain;

public interface IEntityKey
{
    bool IsEmpty { get; }
}

public interface IEntityKey<out TValue> : IEntityKey
    where TValue : notnull
{
    TValue Value { get; }
}
