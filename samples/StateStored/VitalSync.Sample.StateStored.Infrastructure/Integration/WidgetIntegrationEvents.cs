using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using VitalSync.Sample.StateStored.Domain;
using Wolverine.Attributes;

namespace VitalSync.Sample.StateStored.Infrastructure.Integration;

// The [Topic] attribute is mandatory (ADR-0023 amendment): it makes the routing key part of the published
// contract instead of deriving it from the CLR namespace, where a rename would silently break consumer
// bindings. Without a routing rule match, Wolverine discards the message without an error.
//
// Placement is provisional. Stage 3 of the walking skeleton introduces a consumer, and the contract then
// moves to a shared Sample.Contracts project - only then does ADR-0024 have a second consumer to reason about.
[Topic("sample.widget-created")]
public sealed record WidgetCreatedIntegrationEvent(Guid WidgetId, string Name) : IIntegrationEvent;

// Selects which domain events cross the context boundary. Most do not: the mapper returns nothing for
// WidgetRenamed, which stays an internal signal feeding the read model only.
public sealed class WidgetIntegrationEventMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent) => domainEvent switch
    {
        WidgetCreated created => [new WidgetCreatedIntegrationEvent(created.WidgetId.Value, created.Name)],
        _ => [],
    };
}
