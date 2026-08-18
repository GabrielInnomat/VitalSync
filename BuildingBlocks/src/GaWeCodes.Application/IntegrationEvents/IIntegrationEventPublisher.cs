using GaWeCodes.Application.DomainEvents;
using GaWeCodes.Domain.Events;

namespace GaWeCodes.Application.IntegrationEvents;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, DomainEventMetadata metadata, IIntegrationEventSink integrationEventSink, CancellationToken cancellationToken);
}
