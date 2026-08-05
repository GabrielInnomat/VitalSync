namespace BuildingBlocks.Domain.Events;

public interface IDomainEventOwner : IHasDomainEvents
{
    void ClearDomainEvents();
}
