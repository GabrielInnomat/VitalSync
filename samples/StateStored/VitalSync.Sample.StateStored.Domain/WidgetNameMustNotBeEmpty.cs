using BuildingBlocks.Domain;

namespace VitalSync.Sample.StateStored.Domain;

// The slice needs exactly one rule, so that the path
// DomainValidationException -> ExceptionToResultBehavior -> Result.Failure(Validation) -> gRPC status
// is actually exercised end to end (ADR-0009/0017).
public sealed class WidgetNameMustNotBeEmpty(string? name) : IDomainValidationRule
{
    public string Message => "The widget name must not be empty.";

    public bool IsInvalid() => string.IsNullOrWhiteSpace(name);
}
