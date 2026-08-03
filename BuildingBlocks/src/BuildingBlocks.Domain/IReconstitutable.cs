namespace BuildingBlocks.Domain;

public interface IReconstitutable<TSelf>
    where TSelf : IReconstitutable<TSelf>
{
    static abstract TSelf CreateEmpty();
}
