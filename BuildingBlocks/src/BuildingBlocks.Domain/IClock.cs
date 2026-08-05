namespace BuildingBlocks.Domain;

public interface IClock
{
    DateTimeOffset Now { get; }
}
