using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

public sealed record WidgetState(WidgetId Id, string Name, int RenameCount) : IState<WidgetState, WidgetId>
{
    public static WidgetState Empty => new(default, string.Empty, 0);

    public WidgetState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        WidgetCreated created => this with { Id = created.WidgetId, Name = created.Name },
        WidgetRenamed renamed => this with { Name = renamed.Name, RenameCount = renamed.RenameCount },
        _ => this,
    };
}
