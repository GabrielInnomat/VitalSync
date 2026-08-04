using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

public sealed record WidgetPartState(WidgetPartId Id, string Label, int Quantity)
    : EntityState<WidgetPartState, WidgetPartId>
{
    public override WidgetPartState Apply(IDomainEvent domainEvent) => domainEvent switch
    {
        WidgetPartQuantityChanged changed => this with { Quantity = changed.Quantity },
        _ => this,
    };
}
