using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, IIntegrationEventSink integrationEventSink, CancellationToken cancellationToken);
}
