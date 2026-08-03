using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Integration;

public sealed class WidgetIntegrationEventMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent, DomainEventMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return domainEvent switch
        {
            WidgetCreated created =>
                [new WidgetCreatedIntegrationEvent(created.WidgetId.Value, created.Name, metadata.EventId, metadata.OccurredAt)],
            _ => [],
        };
    }
}
