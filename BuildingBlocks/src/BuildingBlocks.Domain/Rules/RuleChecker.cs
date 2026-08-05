namespace BuildingBlocks.Domain.Rules;

public static class RuleChecker
{
    public static void Check(IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.IsBroken())
        {
            throw new BusinessRuleViolationException(rule.Message);
        }
    }

    public static void Check(IDomainValidationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.IsInvalid())
        {
            throw new DomainValidationException(rule.Message);
        }
    }

    public static void Check(params IBusinessRule[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        foreach (var rule in rules)
        {
            Check(rule);
        }
    }

    public static void Check(params IDomainValidationRule[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        foreach (var rule in rules)
        {
            Check(rule);
        }
    }
}
