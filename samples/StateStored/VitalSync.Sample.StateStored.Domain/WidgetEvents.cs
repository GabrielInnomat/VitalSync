using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

public sealed record WidgetCreated(WidgetId WidgetId, string Name) : DomainEvent;

public sealed record WidgetRenamed(WidgetId WidgetId, string Name, int RenameCount) : DomainEvent;
