using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

[EventName("widget-created-v1")]
public sealed record WidgetCreated(WidgetId WidgetId, string Name) : DomainEvent;

[EventName("widget-renamed-v1")]
public sealed record WidgetRenamed(WidgetId WidgetId, string Name, int RenameCount) : DomainEvent;
