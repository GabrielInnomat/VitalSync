namespace VitalSync.Sample.StateStored.Domain;

public sealed record WidgetState(WidgetId Id, string Name, int RenameCount)
    : AggregateState<WidgetState, WidgetId>
{
    public IReadOnlyCollection<WidgetPartState> Parts { get; init; } = new List<WidgetPartState>();

    public static WidgetState Empty => new(default, string.Empty, 0);

    public override WidgetState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        WidgetCreated created => this with { Id = created.WidgetId, Name = created.Name },
        WidgetRenamed renamed => this with { Name = renamed.Name, RenameCount = renamed.RenameCount },
        WidgetPartAdded added => this with
        {
            Parts = Parts.Append(new WidgetPartState(added.PartId, added.Label, added.Quantity)).ToList(),
        },
        WidgetPartQuantityChanged changed => this with
        {
            Parts = Parts
                .Select(part => part.Id == changed.PartId ? part.Apply(changed) : part)
                .ToList(),
        },
        WidgetPartRemoved removed => this with
        {
            Parts = Parts.Where(part => part.Id != removed.PartId).ToList(),
        },
        _ => this,
    };
}
