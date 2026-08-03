using BuildingBlocks.Application;
using Wolverine.Attributes;

namespace VitalSync.Sample.Contracts;

[Topic("sample.widget-created")]
public sealed record WidgetCreatedIntegrationEvent(Guid WidgetId, string Name, Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
