namespace BuildingBlocks.Domain;

public interface IStateOwner
{
    Type StateType { get; }

    object State { get; }

    void Restore(object state);
}
