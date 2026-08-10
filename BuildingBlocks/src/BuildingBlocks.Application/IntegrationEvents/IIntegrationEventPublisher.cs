using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Application.IntegrationEvents;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, DomainEventMetadata metadata, IIntegrationEventSink integrationEventSink, CancellationToken cancellationToken);
}
