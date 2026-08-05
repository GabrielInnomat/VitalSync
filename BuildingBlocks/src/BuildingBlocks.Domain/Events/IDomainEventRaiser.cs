namespace BuildingBlocks.Domain.Events;

public interface IDomainEventRaiser
{
    void Raise(IDomainEvent domainEvent);
}
