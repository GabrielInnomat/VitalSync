using GaWeCodes.Thessera.Application.IntegrationEvents;

namespace VitalSync.Sample.Contracts;

[IntegrationEventTopic("sample-state-stored.widget-created")]
public sealed record WidgetCreatedIntegrationEvent(Guid WidgetId, string Name, Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
