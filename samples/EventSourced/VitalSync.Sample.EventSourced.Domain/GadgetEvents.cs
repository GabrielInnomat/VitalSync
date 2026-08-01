using BuildingBlocks.Domain;

namespace VitalSync.Sample.EventSourced.Domain;

public sealed record GadgetCreated(GadgetId GadgetId, string Name) : DomainEvent;

// Carries the resulting rename count for the same reason as the state-stored sample: delivery is
// at-least-once (ADR-0022), so a projection that incremented a counter itself would drift on redelivery.
// Event sourcing does not remove the need for it - the stream version stays in the event store and never
// reaches the projection handler, which only ever sees the IDomainEvent.
public sealed record GadgetRenamed(GadgetId GadgetId, string Name, int RenameCount) : DomainEvent;

// The event that only exists because the history is the point: a retired gadget keeps everything it ever
// was, and the aggregate can still be replayed as it stood before the retirement.
public sealed record GadgetRetired(GadgetId GadgetId, string Reason) : DomainEvent;
