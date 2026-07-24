namespace BuildingBlocks.Application.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var error = new Error("recipe.name_required", "The recipe name is required.", ErrorCategory.Validation);

        Assert.Equal("recipe.name_required", error.Code);
        Assert.Equal("The recipe name is required.", error.Message);
        Assert.Equal(ErrorCategory.Validation, error.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidCode_ThrowsArgumentException(string? code)
    {
        Assert.Throws<ArgumentException>(() => new Error(code!, "message", ErrorCategory.Validation));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidMessage_ThrowsArgumentException(string? message)
    {
        Assert.Throws<ArgumentException>(() => new Error("code", message!, ErrorCategory.Validation));
    }

    [Fact]
    public void Validation_CreatesErrorWithValidationCategory()
    {
        var error = Error.Validation("code", "message");

        Assert.Equal(ErrorCategory.Validation, error.Category);
    }

    [Fact]
    public void BusinessRule_CreatesErrorWithBusinessRuleCategory()
    {
        var error = Error.BusinessRule("code", "message");

        Assert.Equal(ErrorCategory.BusinessRule, error.Category);
    }

    [Fact]
    public void NotFound_CreatesErrorWithNotFoundCategory()
    {
        var error = Error.NotFound("code", "message");

        Assert.Equal(ErrorCategory.NotFound, error.Category);
    }

    [Fact]
    public void Conflict_CreatesErrorWithConflictCategory()
    {
        var error = Error.Conflict("code", "message");

        Assert.Equal(ErrorCategory.Conflict, error.Category);
    }

    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var a = new Error("code", "message", ErrorCategory.Conflict);
        var b = new Error("code", "message", ErrorCategory.Conflict);

        Assert.Equal(a, b);
    }
}
