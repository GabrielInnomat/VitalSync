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
    public void Check_BusinessRuleParams_ThrowsOnFirstBrokenAndStops()
    {
        var broken = new FakeBusinessRule(isBroken: true, message: "first");
        var never = new FakeBusinessRule(isBroken: true, message: "second");

        var ex = Assert.Throws<BusinessRuleViolationException>(
            () => RuleChecker.Check(broken, never));

        Assert.Equal("first", ex.Message);
        Assert.False(never.Evaluated);
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
    public void Check_ValidationRuleParams_ThrowsOnFirstInvalidAndStops()
    {
        var invalid = new FakeValidationRule(isInvalid: true, message: "first");
        var never = new FakeValidationRule(isInvalid: true, message: "second");

        var ex = Assert.Throws<DomainValidationException>(
            () => RuleChecker.Check(invalid, never));

        Assert.Equal("first", ex.Message);
        Assert.False(never.Evaluated);
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
