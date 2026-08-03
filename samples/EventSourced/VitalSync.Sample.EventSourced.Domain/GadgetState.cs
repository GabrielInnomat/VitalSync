using BuildingBlocks.Domain;

namespace VitalSync.Sample.EventSourced.Domain;

public sealed record GadgetState(GadgetId Id, string Name, int RenameCount, bool IsRetired)
    : AggregateState<GadgetState, GadgetId>
{
    public static GadgetState Empty => new(default, string.Empty, 0, false);

    public override GadgetState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        GadgetCreated created => this with { Id = created.GadgetId, Name = created.Name },
        GadgetRenamed renamed => this with { Name = renamed.Name, RenameCount = renamed.RenameCount },
        GadgetRetired => this with { IsRetired = true },
        _ => this,
    };
}
