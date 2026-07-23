namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a business rule that can be validated.
/// </summary>
/// <remarks>
/// Business rules encapsulate an invariant of the domain as a small, testable, self-describing object, keeping the
/// invariant and its explanation in one place instead of scattering conditionals through the aggregate. Pass instances
/// to <see cref="RuleChecker"/>, which throws a <see cref="BusinessRuleViolationException"/> when the rule is broken.
/// </remarks>
public interface IBusinessRule
{
    /// <summary>
    /// Gets the message associated with the business rule.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Determines whether the business rule is broken.
    /// </summary>
    /// <returns><c>true</c> if the business rule is broken; otherwise, <c>false</c>.</returns>
    bool IsBroken();
}
