using BuildingBlocks.Application;
using Wolverine.Attributes;

namespace VitalSync.Sample.Contracts;

// Moved here from the state-stored service in stage 3, and only now. ADR-0024 places a contract by its
// consumer, not by symmetry: while the state-stored service was the only party that knew this type, keeping
// it next to its mapper was correct. The event-sourced service subscribing to it is what makes it shared.
//
// GadgetRetiredIntegrationEvent deliberately stayed behind in the event-sourced service's Infrastructure -
// nothing consumes it yet. Moving it too would look tidier and would be exactly the reasoning ADR-0024
// rejects.
//
// The routing key is part of the published contract (ADR-0023 amendment). Changing it breaks every consumer
// binding silently, because Wolverine discards a message no queue is bound for without an error.
[Topic("sample.widget-created")]
public sealed record WidgetCreatedIntegrationEvent(Guid WidgetId, string Name) : IIntegrationEvent;
