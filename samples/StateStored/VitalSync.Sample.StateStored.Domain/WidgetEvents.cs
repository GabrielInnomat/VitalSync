using GaWeCodes.Domain.Events;
using GaWeCodes.Domain.Naming;

namespace VitalSync.Sample.StateStored.Domain;

[EventName("widget-created-v1")]
public sealed record WidgetCreated(WidgetId WidgetId, string Name) : DomainEvent;

[EventName("widget-renamed-v1")]
public sealed record WidgetRenamed(WidgetId WidgetId, string Name, int RenameCount) : DomainEvent;

[EventName("widget-part-added-v1")]
public sealed record WidgetPartAdded(WidgetId WidgetId, WidgetPartId PartId, string Label, int Quantity) : DomainEvent;

[EventName("widget-part-quantity-changed-v1")]
public sealed record WidgetPartQuantityChanged(
    WidgetId WidgetId,
    WidgetPartId PartId,
    int Quantity,
    int PreviousQuantity) : DomainEvent;

[EventName("widget-part-removed-v1")]
public sealed record WidgetPartRemoved(WidgetId WidgetId, WidgetPartId PartId, int Quantity) : DomainEvent;
