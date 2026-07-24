namespace BuildingBlocks.Application.Tests;

public sealed class ResultOfTTests
{
    [Fact]
    public void Success_CreatesSuccessfulResultWithValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void SuccessOnBase_CreatesSuccessfulResultWithValue()
    {
        var result = Result.Success("created");

        Assert.True(result.IsSuccess);
        Assert.Equal("created", result.Value);
    }

    [Fact]
    public void Failure_WithError_CreatesFailedResult()
    {
        var error = Error.NotFound("recipe.not_found", "The recipe was not found.");

        var result = Result<int>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, Assert.Single(result.Errors));
    }

    [Fact]
    public void Value_OnFailedResult_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Failure(Error.NotFound("code", "message"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessfulResult()
    {
        Result<int> result = 7;

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailedResult()
    {
        Result<int> result = Error.Validation("code", "message");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.Validation, Assert.Single(result.Errors).Category);
    }

    [Fact]
    public void Failure_WithNullError_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure((Error)null!));
    }
}
