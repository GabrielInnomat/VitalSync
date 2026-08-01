using BuildingBlocks.Domain;

namespace VitalSync.Sample.EventSourced.Domain;

// Input shape - the same rule as in the state-stored sample, so the path
// DomainValidationException -> ExceptionToResultBehavior -> Result.Failure(Validation) -> gRPC status
// is exercised on this side too (ADR-0009/0017).
public sealed class GadgetNameMustNotBeEmpty(string? name) : IDomainValidationRule
{
    public string Message => "The gadget name must not be empty.";

    public bool IsInvalid() => string.IsNullOrWhiteSpace(name);
}

// A business rule rather than a validation rule (ADR-0009): it is not about the shape of the input but
// about the state the aggregate is in, and it translates to FailureCategory.BusinessRule instead of
// Validation. The state-stored sample has no equivalent, so this is where the two slices deliberately
// differ: it proves the second half of the ADR-0017 translation on the event-sourced side.
public sealed class RetiredGadgetMustNotChange(bool isRetired) : IBusinessRule
{
    public string Message => "A retired gadget can no longer be changed.";

    public bool IsBroken() => isRetired;
}
