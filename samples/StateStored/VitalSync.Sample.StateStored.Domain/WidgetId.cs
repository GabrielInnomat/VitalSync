using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

public readonly record struct WidgetId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static WidgetId New() => new(Guid.NewGuid());
}
