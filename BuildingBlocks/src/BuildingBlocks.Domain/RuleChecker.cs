namespace BuildingBlocks.Domain;

/// <summary>
/// Provides utility methods for checking business rules and domain validation rules.
/// </summary>
public static class RuleChecker
{
    /// <summary>
    /// Checks the specified business rule and throws a <see cref="BusinessRuleViolationException"/> if the rule is broken.
    /// </summary>
    /// <param name="rule">The business rule to check. A <see langword="null"/> rule is ignored.</param>
    /// <exception cref="BusinessRuleViolationException">Thrown if <paramref name="rule"/> is broken.</exception>
    public static void Check(IBusinessRule rule)
    {
        if (rule?.IsBroken() == true)
        {
            throw new BusinessRuleViolationException(rule.Message);
        }
    }

    /// <summary>
    /// Checks the specified domain validation rule and throws a <see cref="DomainValidationException"/> if the rule is invalid.
    /// </summary>
    /// <param name="rule">The domain validation rule to check. A <see langword="null"/> rule is ignored.</param>
    /// <exception cref="DomainValidationException">Thrown if <paramref name="rule"/> is invalid.</exception>
    public static void Check(IDomainValidationRule rule)
    {
        if (rule?.IsInvalid() == true)
        {
            throw new DomainValidationException(rule.Message);
        }
    }

    /// <summary>
    /// Checks the specified business rules and throws a <see cref="BusinessRuleViolationException"/> if any of the rules are broken.
    /// </summary>
    /// <param name="rules">The business rules to check. A <see langword="null"/> array or <see langword="null"/> rule is ignored.</param>
    /// <exception cref="BusinessRuleViolationException">Thrown if any of the <paramref name="rules"/> are broken.</exception>
    public static void Check(params IBusinessRule[] rules)
    {
        foreach (var rule in rules ?? [])
        {
            Check(rule);
        }
    }

    /// <summary>
    /// Checks the specified domain validation rules and throws a <see cref="DomainValidationException"/> if any of the rules are invalid.
    /// </summary>
    /// <param name="rules">The domain validation rules to check. A <see langword="null"/> array or <see langword="null"/> rule is ignored.</param>
    /// <exception cref="DomainValidationException">Thrown if any of the <paramref name="rules"/> are invalid.</exception>
    public static void Check(params IDomainValidationRule[] rules)
    {
        foreach (var rule in rules ?? [])
        {
            Check(rule);
        }
    }
}
