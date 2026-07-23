namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a validation rule for a domain entity.
/// </summary>
/// <remarks>
/// A domain validation rule captures a structural precondition that must hold for domain data to be well formed,
/// separate from the business invariants modelled by <see cref="IBusinessRule"/>. Pass instances to
/// <see cref="RuleChecker"/>, which throws a <see cref="DomainValidationException"/> when the rule is invalid.
/// </remarks>
public interface IDomainValidationRule
{
    /// <summary>
    /// Gets the error message associated with the validation rule.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Determines whether the validation rule is invalid.
    /// </summary>
    /// <returns><c>true</c> if the validation rule is invalid; otherwise, <c>false</c>.</returns>
    bool IsInvalid();
}
