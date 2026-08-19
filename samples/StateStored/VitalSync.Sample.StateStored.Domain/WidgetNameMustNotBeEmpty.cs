using GaWeCodes.Domain.Rules;

namespace VitalSync.Sample.StateStored.Domain;

public sealed class WidgetNameMustNotBeEmpty(string? name) : IDomainValidationRule
{
    public string Code => "widget.name.required";

    public string? Target => "name";

    public string Message => "The widget name must not be empty.";

    public bool IsInvalid() => string.IsNullOrWhiteSpace(name);
}
