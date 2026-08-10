using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Application.IntegrationEvents;

public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    IReadOnlyCollection<IIntegrationEvent> Map(TDomainEvent domainEvent, DomainEventMetadata metadata);
}
