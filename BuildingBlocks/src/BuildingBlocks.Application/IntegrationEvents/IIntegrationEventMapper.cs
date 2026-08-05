using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Application.IntegrationEvents;

public interface IIntegrationEventMapper
{
    IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent, DomainEventMetadata metadata);
}
