using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using VitalSync.Sample.EventSourced.Domain;
using Wolverine.Attributes;

namespace VitalSync.Sample.EventSourced.Infrastructure.Integration;

[Topic("sample.gadget-retired")]
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
