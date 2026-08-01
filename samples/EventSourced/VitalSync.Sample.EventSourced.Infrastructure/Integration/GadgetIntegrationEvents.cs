using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using VitalSync.Sample.EventSourced.Domain;
using Wolverine.Attributes;

namespace VitalSync.Sample.EventSourced.Infrastructure.Integration;

// [Topic] is mandatory (ADR-0023 amendment): without a matching routing rule Wolverine discards the message
// silently, and deriving the key from the CLR namespace would let a rename break consumer bindings.
// Placement is provisional, as on the state-stored side - stage 3 gives it a consumer and moves it.
[Topic("sample.gadget-retired")]
public sealed record GadgetRetiredIntegrationEvent(Guid GadgetId, string Reason) : IIntegrationEvent;

// Which domain events cross the boundary is a service decision. Here only the retirement does: creation and
// renaming are this context's internal business, whereas a retired gadget is something other contexts have
// to stop referring to.
public sealed class GadgetIntegrationEventMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent) => domainEvent switch
    {
        GadgetRetired retired => [new GadgetRetiredIntegrationEvent(retired.GadgetId.Value, retired.Reason)],
        _ => [],
    };
}
