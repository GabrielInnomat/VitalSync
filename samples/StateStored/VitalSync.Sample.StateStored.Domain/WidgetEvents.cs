using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

public sealed record WidgetCreated(WidgetId WidgetId, string Name) : DomainEvent;

// Carries the resulting rename count rather than implying an increment: delivery is at-least-once
// (ADR-0022), so a projection that incremented a counter itself would drift on redelivery.
public sealed record WidgetRenamed(WidgetId WidgetId, string Name, int RenameCount) : DomainEvent;
