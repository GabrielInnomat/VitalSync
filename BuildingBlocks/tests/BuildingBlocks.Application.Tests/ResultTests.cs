namespace BuildingBlocks.Application.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WithError_CreatesFailedResult()
    {
        var error = Error.NotFound("recipe.not_found", "The recipe was not found.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(error, Assert.Single(result.Errors));
    }

    [Fact]
    public void Failure_WithMultipleErrors_CarriesAllErrors()
    {
        var errors = new[]
        {
            Error.Validation("recipe.name_required", "The recipe name is required."),
            Error.Validation("recipe.name_too_long", "The recipe name is too long."),
        };

        var result = Result.Failure(errors);

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Failure_WithNullError_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure((Error)null!));
    }

    [Fact]
    public void Failure_WithEmptyErrors_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Array.Empty<Error>()));
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailedResult()
    {
        Result result = Error.Conflict("recipe.exists", "The recipe already exists.");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.Conflict, Assert.Single(result.Errors).Category);
    }
}
