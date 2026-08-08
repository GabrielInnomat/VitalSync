using BuildingBlocks.Domain.Rules;
using BuildingBlocks.Domain.Tests.TestDoubles;

namespace BuildingBlocks.Domain.Tests;

public sealed class RuleCheckerTests
{
    [Fact]
    public void Check_BrokenBusinessRule_ThrowsWithMessage()
    {
        var rule = new FakeBusinessRule(isBroken: true, message: "nope");

        var ex = Assert.Throws<BusinessRuleViolationException>(() => RuleChecker.Check(rule));
        Assert.Equal("nope", ex.Message);
    }

    [Fact]
    public void Check_SatisfiedBusinessRule_DoesNotThrow()
    {
        var rule = new FakeBusinessRule(isBroken: false);

        RuleChecker.Check(rule);

        Assert.True(rule.Evaluated);
    }

    [Fact]
    public void Check_InvalidValidationRule_ThrowsWithMessage()
    {
        var rule = new FakeValidationRule(isInvalid: true, message: "bad");

        var ex = Assert.Throws<DomainValidationException>(() => RuleChecker.Check(rule));
        Assert.Equal("bad", ex.Message);
    }

    [Fact]
    public void Check_ValidValidationRule_DoesNotThrow()
    {
        var rule = new FakeValidationRule(isInvalid: false);

        RuleChecker.Check(rule);

        Assert.True(rule.Evaluated);
    }

    [Fact]
    public void Check_BusinessRuleParams_EvaluatesEveryRuleAndCollectsAll()
    {
        var broken = new FakeBusinessRule(isBroken: true, message: "first", code: "a");
        var alsoBroken = new FakeBusinessRule(isBroken: true, message: "second", code: "b");

        var ex = Assert.Throws<BusinessRuleViolationException>(
            () => RuleChecker.Check(broken, alsoBroken));

        Assert.True(alsoBroken.Evaluated);
        Assert.Equal(2, ex.Violations.Count);
        Assert.Equal("a", ex.Violations[0].Code);
        Assert.Equal("first", ex.Violations[0].Message);
        Assert.Equal("b", ex.Violations[1].Code);
        Assert.Equal("second", ex.Violations[1].Message);
        Assert.Contains("first", ex.Message, StringComparison.Ordinal);
        Assert.Contains("second", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_BrokenBusinessRule_CarriesTheRulesOwnCodeAndNoTarget()
    {
        var broken = new FakeBusinessRule(isBroken: true, message: "first", code: "recipe.already_published");

        var ex = Assert.Throws<BusinessRuleViolationException>(() => RuleChecker.Check(broken));

        var violation = Assert.Single(ex.Violations);
        Assert.Equal("recipe.already_published", violation.Code);
        Assert.Null(violation.Target);
    }

    [Fact]
    public void Check_BusinessRuleParams_AllSatisfied_DoesNotThrow()
    {
        var a = new FakeBusinessRule(isBroken: false);
        var b = new FakeBusinessRule(isBroken: false);

        RuleChecker.Check(a, b);

        Assert.True(a.Evaluated);
        Assert.True(b.Evaluated);
    }

    [Fact]
    public void Check_ValidationRuleParams_EvaluatesEveryRuleAndCollectsAll()
    {
        var invalid = new FakeValidationRule(isInvalid: true, message: "first", code: "a", target: "name");
        var alsoInvalid = new FakeValidationRule(isInvalid: true, message: "second", code: "b", target: "quantity");

        var ex = Assert.Throws<DomainValidationException>(
            () => RuleChecker.Check(invalid, alsoInvalid));

        Assert.True(alsoInvalid.Evaluated);
        Assert.Equal(2, ex.Violations.Count);
        Assert.Equal("a", ex.Violations[0].Code);
        Assert.Equal("name", ex.Violations[0].Target);
        Assert.Equal("first", ex.Violations[0].Message);
        Assert.Equal("b", ex.Violations[1].Code);
        Assert.Equal("quantity", ex.Violations[1].Target);
        Assert.Equal("second", ex.Violations[1].Message);
    }

    [Fact]
    public void Check_ValidationRuleParams_OnlyOneInvalid_KeepsTheRuleMessageVerbatim()
    {
        var valid = new FakeValidationRule(isInvalid: false);
        var invalid = new FakeValidationRule(isInvalid: true, message: "bad", target: "name");

        var ex = Assert.Throws<DomainValidationException>(() => RuleChecker.Check(valid, invalid));

        Assert.Equal("bad", ex.Message);
        var violation = Assert.Single(ex.Violations);
        Assert.Equal("name", violation.Target);
    }

    [Fact]
    public void Check_ValidationRuleParams_AllValid_DoesNotThrow()
    {
        var a = new FakeValidationRule(isInvalid: false);
        var b = new FakeValidationRule(isInvalid: false);

        RuleChecker.Check(a, b);

        Assert.True(a.Evaluated);
        Assert.True(b.Evaluated);
    }

    [Fact]
    public void Check_NullBusinessRule_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RuleChecker.Check((IBusinessRule)null!));
    }

    [Fact]
    public void Check_NullValidationRule_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RuleChecker.Check((IDomainValidationRule)null!));
    }

    [Fact]
    public void Check_NullBusinessRuleArray_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RuleChecker.Check((IBusinessRule[])null!));
    }

    [Fact]
    public void Check_NullValidationRuleArray_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RuleChecker.Check((IDomainValidationRule[])null!));
    }

    [Fact]
    public void Check_NullRuleAmongBusinessRules_ThrowsAndDoesNotEvaluateLaterRules()
    {
        var later = new FakeBusinessRule(isBroken: true, message: "later");

        Assert.Throws<ArgumentNullException>(() => RuleChecker.Check(null!, later));

        Assert.False(later.Evaluated);
    }

    [Fact]
    public void Check_NullRuleAmongValidationRules_ThrowsAndDoesNotEvaluateLaterRules()
    {
        var later = new FakeValidationRule(isInvalid: true, message: "later");

        Assert.Throws<ArgumentNullException>(() => RuleChecker.Check(null!, later));

        Assert.False(later.Evaluated);
    }

    [Fact]
    public void Check_EmptyBusinessRuleParams_DoesNotThrow()
    {
        RuleChecker.Check(Array.Empty<IBusinessRule>());
    }

    [Fact]
    public void Check_EmptyValidationRuleParams_DoesNotThrow()
    {
        RuleChecker.Check(Array.Empty<IDomainValidationRule>());
    }
}
