using GaWeCodes.Thessera.Application.DomainEvents;
using GaWeCodes.Thessera.Application.IntegrationEvents;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Infrastructure.Integration;

[IntegrationEventTopic("sample-event-sourced.gadget-retired")]
public sealed record GadgetRetiredIntegrationEvent(Guid GadgetId, string Reason, Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed class GadgetIntegrationEventMapper : IIntegrationEventMapper<GadgetRetired>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(GadgetRetired domainEvent, DomainEventMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(metadata);

        return [new GadgetRetiredIntegrationEvent(domainEvent.GadgetId.Value, domainEvent.Reason, metadata.EventId, metadata.OccurredAt)];
    }
}
