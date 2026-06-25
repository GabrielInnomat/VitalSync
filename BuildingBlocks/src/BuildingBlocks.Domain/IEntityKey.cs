namespace BuildingBlocks.Domain;

public interface IEntityKey
{
    Guid Value { get; }
}