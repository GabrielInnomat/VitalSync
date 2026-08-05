using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Application.DomainEvents;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, DomainEventMetadata metadata, IIntegrationEventSink integrationEventSink, CancellationToken cancellationToken);
}
