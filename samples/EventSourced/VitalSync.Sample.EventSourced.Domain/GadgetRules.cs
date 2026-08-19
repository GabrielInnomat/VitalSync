using GaWeCodes.Domain.Rules;

namespace VitalSync.Sample.EventSourced.Domain;

public sealed class GadgetNameMustNotBeEmpty(string? name) : IDomainValidationRule
{
    public string Code => "gadget.name.required";

    public string? Target => "name";

    public string Message => "The gadget name must not be empty.";

    public bool IsInvalid() => string.IsNullOrWhiteSpace(name);
}

public sealed class RetiredGadgetMustNotChange(bool isRetired) : IBusinessRule
{
    public string Code => "gadget.retired";

    public string Message => "A retired gadget can no longer be changed.";

    public bool IsBroken() => isRetired;
}

public sealed class GadgetComponentLabelMustNotBeEmpty(string? label) : IDomainValidationRule
{
    public string Code => "gadget.component.label.required";

    public string? Target => "label";

    public string Message => "The gadget component label must not be empty.";

    public bool IsInvalid() => string.IsNullOrWhiteSpace(label);
}

public sealed class GadgetComponentMustExist(
    IReadOnlyCollection<GadgetComponentState> components,
    GadgetComponentId componentId) : IBusinessRule
{
    public string Code => "gadget.component.not_found";

    public string Message => $"The gadget has no component with id '{componentId.Value}'.";

    public bool IsBroken() => !components.Any(component => component.Id == componentId);
}
