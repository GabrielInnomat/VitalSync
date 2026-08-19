using GaWeCodes.Domain.Rules;

namespace VitalSync.Sample.StateStored.Domain;

public sealed class WidgetPartLabelMustNotBeEmpty(string? label) : IDomainValidationRule
{
    public string Code => "widget.part.label.required";

    public string? Target => "label";

    public string Message => "The widget part label must not be empty.";

    public bool IsInvalid() => string.IsNullOrWhiteSpace(label);
}

public sealed class WidgetPartQuantityMustBePositive(int quantity) : IDomainValidationRule
{
    public string Code => "widget.part.quantity.positive";

    public string? Target => "quantity";

    public string Message => "The widget part quantity must be greater than zero.";

    public bool IsInvalid() => quantity <= 0;
}

public sealed class WidgetPartMustExist(IReadOnlyCollection<WidgetPartState> parts, WidgetPartId partId) : IBusinessRule
{
    public string Code => "widget.part.not_found";

    public string Message => $"The widget has no part with id '{partId.Value}'.";

    public bool IsBroken() => !parts.Any(part => part.Id == partId);
}
