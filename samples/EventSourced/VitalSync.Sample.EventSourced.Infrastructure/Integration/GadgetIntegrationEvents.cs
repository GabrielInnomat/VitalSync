using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Domain.Events;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Infrastructure.Integration;

[IntegrationEventTopic("sample-event-sourced.gadget-retired")]
public sealed record GadgetRetiredIntegrationEvent(Guid GadgetId, string Reason, Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed class GadgetIntegrationEventMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent, DomainEventMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return domainEvent switch
        {
            GadgetRetired retired =>
                [new GadgetRetiredIntegrationEvent(retired.GadgetId.Value, retired.Reason, metadata.EventId, metadata.OccurredAt)],
            _ => [],
        };
    }
}
