using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Domain.Events;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Integration;

public sealed class WidgetIntegrationEventMapper : IIntegrationEventMapper<WidgetCreated>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(WidgetCreated domainEvent, DomainEventMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

        return [new WidgetCreatedIntegrationEvent(domainEvent.WidgetId.Value, domainEvent.Name, metadata.EventId, metadata.OccurredAt)];
    }
}
