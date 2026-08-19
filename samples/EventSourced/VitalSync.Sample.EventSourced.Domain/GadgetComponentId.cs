using GaWeCodes.Thessera.Domain.Entities;

namespace VitalSync.Sample.EventSourced.Domain;

public readonly record struct GadgetComponentId(Guid Value) : IEntityKey<Guid>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static GadgetComponentId New() => new(Guid.NewGuid());
}
