using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Infrastructure.Integration;

// The contract itself now lives in VitalSync.Sample.Contracts, because the event-sourced service consumes it
// (ADR-0024). What stays here is the mapper: which of this context's domain events cross the boundary is the
// publishing service's decision and nobody else's.
//
// Most events do not cross: the mapper returns nothing for WidgetRenamed, which remains an internal signal
// feeding the read model only.
public sealed class WidgetIntegrationEventMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent) => domainEvent switch
    {
        WidgetCreated created => [new WidgetCreatedIntegrationEvent(created.WidgetId.Value, created.Name)],
        _ => [],
    };
}
