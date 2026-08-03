using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

public sealed class WidgetNameMustNotBeEmpty(string? name) : IDomainValidationRule
{
    public string Message => "The widget name must not be empty.";

    public bool IsInvalid() => string.IsNullOrWhiteSpace(name);
}
