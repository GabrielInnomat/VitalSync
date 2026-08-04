namespace BuildingBlocks.Domain;

public interface IDomainEventRaiser
{
    void Raise(IDomainEvent domainEvent);
}
