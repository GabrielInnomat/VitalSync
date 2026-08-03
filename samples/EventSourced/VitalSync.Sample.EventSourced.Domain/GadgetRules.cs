using BuildingBlocks.Domain;

namespace VitalSync.Sample.EventSourced.Domain;

public sealed class GadgetNameMustNotBeEmpty(string? name) : IDomainValidationRule
{
    public string Message => "The gadget name must not be empty.";

    public bool IsInvalid() => string.IsNullOrWhiteSpace(name);
}

public sealed class RetiredGadgetMustNotChange(bool isRetired) : IBusinessRule
{
    public string Message => "A retired gadget can no longer be changed.";

    public bool IsBroken() => isRetired;
}
