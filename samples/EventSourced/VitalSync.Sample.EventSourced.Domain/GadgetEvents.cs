using BuildingBlocks.Domain;

namespace VitalSync.Sample.EventSourced.Domain;

public sealed record GadgetCreated(GadgetId GadgetId, string Name) : DomainEvent;

public sealed record GadgetRenamed(GadgetId GadgetId, string Name, int RenameCount) : DomainEvent;

public sealed record GadgetRetired(GadgetId GadgetId, string Reason) : DomainEvent;
