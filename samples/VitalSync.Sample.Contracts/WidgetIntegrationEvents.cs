using BuildingBlocks.Application;

namespace VitalSync.Sample.Contracts;

[IntegrationEventTopic("sample.widget-created")]
public sealed record WidgetCreatedIntegrationEvent(Guid WidgetId, string Name, Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
