using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

public sealed class WidgetPartLabelMustNotBeEmpty(string? label) : IDomainValidationRule
{
    public string Message => "The widget part label must not be empty.";

    public bool IsInvalid() => string.IsNullOrWhiteSpace(label);
}

public sealed class WidgetPartQuantityMustBePositive(int quantity) : IDomainValidationRule
{
    public string Message => "The widget part quantity must be greater than zero.";

    public bool IsInvalid() => quantity <= 0;
}

public sealed class WidgetPartMustExist(IReadOnlyCollection<WidgetPart> parts, WidgetPartId partId) : IBusinessRule
{
    public string Message => $"The widget has no part with id '{partId.Value}'.";

    public bool IsBroken() => !parts.Any(part => part.Id == partId);
}
