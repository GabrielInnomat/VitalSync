namespace BuildingBlocks.Application.Tests;

public sealed class ResultOfTTests
{
    [Fact]
    public void Success_CreatesSuccessfulResultWithValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void SuccessOnBase_CreatesSuccessfulResultWithValue()
    {
        var result = Result.Success("created");

        Assert.True(result.IsSuccess);
        Assert.Equal("created", result.Value);
    }

    [Fact]
    public void Failure_WithDescription_CreatesFailedResult()
    {
        var failure = Failure.NotFound("recipe.not_found", "The recipe was not found.");

        var result = Result<int>.Failure(failure);

        Assert.True(result.IsFailure);
        Assert.Equal(failure, Assert.Single(result.Failures));
    }

    [Fact]
    public void Value_OnFailedResult_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Failure(Failure.NotFound("code", "message"));

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
    public void ImplicitConversion_FromFailure_CreatesFailedResult()
    {
        Result<int> result = Failure.Validation("code", "message");

        Assert.True(result.IsFailure);
        Assert.Equal(FailureCategory.Validation, Assert.Single(result.Failures).Category);
    }

    [Fact]
    public void Failure_WithNullDescription_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure((Failure)null!));
    }
}
