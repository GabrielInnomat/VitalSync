using GaWeCodes.Domain.Aggregates;
using GaWeCodes.Domain.Events;

namespace VitalSync.Sample.EventSourced.Domain;

public sealed record GadgetState(GadgetId Id, string Name, int RenameCount, bool IsRetired)
    : AggregateState<GadgetState, GadgetId>
{
    public IReadOnlyCollection<GadgetComponentState> Components { get; init; } =
        new List<GadgetComponentState>();

    public static GadgetState Empty => new(default, string.Empty, 0, false);

    public override GadgetState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        GadgetCreated created => this with { Id = created.GadgetId, Name = created.Name },
        GadgetRenamed renamed => this with { Name = renamed.Name, RenameCount = renamed.RenameCount },
        GadgetRetired => this with { IsRetired = true },
        GadgetComponentAdded added => this with
        {
            Components = Components
                .Append(new GadgetComponentState(added.ComponentId, added.Label))
                .ToList(),
        },
        GadgetComponentRelabelled relabelled => this with
        {
            Components = Components
                .Select(component =>
                    component.Id == relabelled.ComponentId ? component.Apply(relabelled) : component)
                .ToList(),
        },
        _ => this,
    };
}
