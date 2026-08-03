using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, DomainEventMetadata metadata, IIntegrationEventSink integrationEventSink, CancellationToken cancellationToken);
}
