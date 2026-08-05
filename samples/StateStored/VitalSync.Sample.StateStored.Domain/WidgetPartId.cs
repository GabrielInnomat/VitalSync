namespace VitalSync.Sample.StateStored.Domain;

public readonly record struct WidgetPartId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static WidgetPartId New() => new(Guid.NewGuid());
}
