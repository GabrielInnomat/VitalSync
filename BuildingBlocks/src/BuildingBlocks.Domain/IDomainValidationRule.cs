namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a validation rule for a domain entity.
/// </summary>
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
