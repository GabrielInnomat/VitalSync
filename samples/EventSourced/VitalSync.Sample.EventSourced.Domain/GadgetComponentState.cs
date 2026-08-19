using GaWeCodes.Thessera.Domain.Entities;
using GaWeCodes.Thessera.Domain.Events;

namespace VitalSync.Sample.EventSourced.Domain;

public sealed record GadgetComponentState(GadgetComponentId Id, string Label)
    : EntityState<GadgetComponentState, GadgetComponentId>
{
    public override GadgetComponentState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        GadgetComponentRelabelled relabelled => this with { Label = relabelled.Label },
        _ => this,
    };
}
