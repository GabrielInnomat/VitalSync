using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using VitalSync.Sample.EventSourced.Domain;
using Wolverine.Attributes;

namespace VitalSync.Sample.EventSourced.Infrastructure.Integration;

[Topic("sample.gadget-retired")]
public sealed record GadgetRetiredIntegrationEvent(Guid GadgetId, string Reason) : IIntegrationEvent;

public sealed class GadgetIntegrationEventMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent) => domainEvent switch
    {
        GadgetRetired retired => [new GadgetRetiredIntegrationEvent(retired.GadgetId.Value, retired.Reason)],
        _ => [],
    };
}
