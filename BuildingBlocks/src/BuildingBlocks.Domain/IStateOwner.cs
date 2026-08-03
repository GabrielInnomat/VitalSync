namespace BuildingBlocks.Domain;

public interface IStateOwner
{
    Type StateType { get; }

    object State { get; }

    long Version { get; }

    void Restore(object state);
}
