using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.Dispatching;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class FailureResultsTests
{
    [Fact]
    public void Create_ForResult_ReturnsFailedResultOfRuntimeType()
    {
        var failure = Failure.NotFound("thing.not_found", "Not found.");

        var result = FailureResults.Create<Result>(failure);

        Assert.IsType<Result>(result);
        Assert.True(result.IsFailure);
        Assert.Same(failure, Assert.Single(result.Failures));
    }

    [Fact]
    public void Create_ForResultOfT_ReturnsFailedResultOfTRuntimeType()
    {
        var failure = Failure.Conflict("thing.conflict", "Conflict.");

        var result = FailureResults.Create<Result<int>>(failure);

        Assert.IsType<Result<int>>(result);
        Assert.True(result.IsFailure);
        Assert.Equal(FailureCategory.Conflict, Assert.Single(result.Failures).Category);
    }
}
