namespace BuildingBlocks.Domain;

public interface IDomainEventsManager : IHasDomainEvents
{
    void ClearDomainEvents();
}
