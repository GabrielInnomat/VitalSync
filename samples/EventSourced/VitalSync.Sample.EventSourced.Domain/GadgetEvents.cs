namespace VitalSync.Sample.EventSourced.Domain;

[EventName("gadget-created-v1")]
public sealed record GadgetCreated(GadgetId GadgetId, string Name) : DomainEvent;

[EventName("gadget-renamed-v1")]
public sealed record GadgetRenamed(GadgetId GadgetId, string Name, int RenameCount) : DomainEvent;

[EventName("gadget-retired-v1")]
public sealed record GadgetRetired(GadgetId GadgetId, string Reason) : DomainEvent;

[EventName("gadget-component-added-v1")]
public sealed record GadgetComponentAdded(
    GadgetId GadgetId,
    GadgetComponentId ComponentId,
    string Label) : DomainEvent;

[EventName("gadget-component-relabelled-v1")]
public sealed record GadgetComponentRelabelled(
    GadgetId GadgetId,
    GadgetComponentId ComponentId,
    string Label) : DomainEvent;
