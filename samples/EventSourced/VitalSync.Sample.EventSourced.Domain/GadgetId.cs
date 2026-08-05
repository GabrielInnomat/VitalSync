namespace VitalSync.Sample.EventSourced.Domain;

public readonly record struct GadgetId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static GadgetId New() => new(Guid.NewGuid());
}
