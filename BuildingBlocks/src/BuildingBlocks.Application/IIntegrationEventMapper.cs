using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

public interface IIntegrationEventMapper
{
    IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent, DomainEventMetadata metadata);
}
