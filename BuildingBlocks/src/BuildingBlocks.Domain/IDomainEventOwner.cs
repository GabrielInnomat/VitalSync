namespace BuildingBlocks.Domain;

public interface IDomainEventOwner : IHasDomainEvents
{
    void ClearDomainEvents();
}
