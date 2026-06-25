namespace BuildingBlocks.Domain;

public static class RuleChecker
{
    public static void Check(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleViolationException(rule.Message);
        }
    }

    public static void Check(IDomainValidationRule rule)
    {
        if (rule.IsInvalid())
        {
            throw new DomainValidationException(rule.Message);
        }
    }

    public static void Check(params IBusinessRule[] rules)
    {
        foreach (var rule in rules)
        {
            Check(rule);
        }
    }
}