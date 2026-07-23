namespace BuildingBlocks.Domain;

/// <summary>
/// Provides a utility class for checking business rules and domain validation rules.
/// </summary>
public static class RuleChecker
{
    /// <summary>
    /// Checks the specified business rule and throws a BusinessRuleViolationException if the rule is broken.
    /// </summary>
    /// <param name="rule">The business rule to check.</param>
    /// <exception cref="BusinessRuleViolationException">Thrown if <paramref name="rule"/> is broken.</exception>
    public static void Check(IBusinessRule rule)
    {
        if (rule?.IsBroken() == true)
        {
            throw new BusinessRuleViolationException(rule.Message);
        }
    }

    /// <summary>
    /// Checks the specified domain validation rule and throws a DomainValidationException if the rule is invalid.
    /// </summary>
    /// <param name="rule">The domain validation rule to check.</param>
    /// <exception cref="DomainValidationException">Thrown if <paramref name="rule"/> is invalid.</exception>
    public static void Check(IDomainValidationRule rule)
    {
        if (rule?.IsInvalid() == true)
        {
            throw new DomainValidationException(rule.Message);
        }
    }

    /// <summary>
    /// Checks the specified business rules and throws a BusinessRuleViolationException if any of the rules are broken.
    /// </summary>
    /// <param name="rules">The business rules to check.</param>
    /// <exception cref="BusinessRuleViolationException">Thrown if any of the <paramref name="rules"/> are broken.</exception>
    public static void Check(params IBusinessRule[] rules)
    {
        foreach (var rule in rules ?? [])
        {
            Check(rule);
        }
    }

    /// <summary>
    /// Checks the specified domain validation rules and throws a DomainValidationException if any of the rules are invalid.
    /// </summary>
    /// <param name="rules">The validation rules to check.</param>
    /// <exception cref="DomainValidationException">Thrown if any of the <paramref name="rules"/> are invalid.</exception>
    public static void Check(params IDomainValidationRule[] rules)
    {
        foreach (var rule in rules ?? [])
        {
            Check(rule);
        }
    }
}
