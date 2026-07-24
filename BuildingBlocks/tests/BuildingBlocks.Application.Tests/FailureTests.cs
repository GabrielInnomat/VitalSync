namespace BuildingBlocks.Application.Tests;

public sealed class FailureTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var failure = new Failure("recipe.name_required", "The recipe name is required.", FailureCategory.Validation);

        Assert.Equal("recipe.name_required", failure.Code);
        Assert.Equal("The recipe name is required.", failure.Message);
        Assert.Equal(FailureCategory.Validation, failure.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidCode_ThrowsArgumentException(string? code)
    {
        Assert.Throws<ArgumentException>(() => new Failure(code!, "message", FailureCategory.Validation));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidMessage_ThrowsArgumentException(string? message)
    {
        Assert.Throws<ArgumentException>(() => new Failure("code", message!, FailureCategory.Validation));
    }

    [Fact]
    public void Validation_CreatesFailureWithValidationCategory()
    {
        var failure = Failure.Validation("code", "message");

        Assert.Equal(FailureCategory.Validation, failure.Category);
    }

    [Fact]
    public void BusinessRule_CreatesFailureWithBusinessRuleCategory()
    {
        var failure = Failure.BusinessRule("code", "message");

        Assert.Equal(FailureCategory.BusinessRule, failure.Category);
    }

    [Fact]
    public void NotFound_CreatesFailureWithNotFoundCategory()
    {
        var failure = Failure.NotFound("code", "message");

        Assert.Equal(FailureCategory.NotFound, failure.Category);
    }

    [Fact]
    public void Conflict_CreatesFailureWithConflictCategory()
    {
        var failure = Failure.Conflict("code", "message");

        Assert.Equal(FailureCategory.Conflict, failure.Category);
    }

    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var a = new Failure("code", "message", FailureCategory.Conflict);
        var b = new Failure("code", "message", FailureCategory.Conflict);

        Assert.Equal(a, b);
    }
}
