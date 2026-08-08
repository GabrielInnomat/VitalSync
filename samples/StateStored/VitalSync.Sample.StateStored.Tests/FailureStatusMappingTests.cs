using BuildingBlocks.Application.Results;
using Grpc.Core;
using VitalSync.Sample.StateStored.Api;

namespace VitalSync.Sample.StateStored.Tests;

public sealed class FailureStatusMappingTests
{
    public static TheoryData<FailureCategory, StatusCode> Mappings => new()
    {
        { FailureCategory.Validation, StatusCode.InvalidArgument },
        { FailureCategory.NotFound, StatusCode.NotFound },
        { FailureCategory.Conflict, StatusCode.Aborted },
        { FailureCategory.BusinessRule, StatusCode.FailedPrecondition },
        { FailureCategory.Forbidden, StatusCode.PermissionDenied },
    };

    [Theory]
    [MemberData(nameof(Mappings))]
    public void ToStatusCode_ForADeclaredCategory_ReturnsTheAgreedStatus(
        FailureCategory category,
        StatusCode expected) =>
        Assert.Equal(expected, FailureStatusMapping.ToStatusCode(category));

    [Fact]
    public void EveryDeclaredCategory_IsMappedToSomethingOtherThanUnknown()
    {
        var unmapped = Enum.GetValues<FailureCategory>()
            .Where(static category => FailureStatusMapping.ToStatusCode(category) == StatusCode.Unknown)
            .ToList();

        Assert.True(
            unmapped.Count == 0,
            $"The gRPC adapter falls through to StatusCode.Unknown for: {string.Join(", ", unmapped)}. A switch over an enum always needs a discard arm, so the compiler cannot report this - add the category here and to the mapping.");
    }

    [Fact]
    public void ToStatusCode_ForAnUndeclaredCategory_FallsBackToUnknown() =>
        Assert.Equal(StatusCode.Unknown, FailureStatusMapping.ToStatusCode((FailureCategory)int.MaxValue));

    [Fact]
    public void ToRpcException_CarriesTheCodeAndMessageOfTheFirstFailure()
    {
        var result = Result.Failed(Failure.NotFound("widget.not_found", "No such widget."));

        var exception = FailureStatusMapping.ToRpcException(result);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        Assert.Equal("widget.not_found: No such widget.", exception.Status.Detail);
    }
}
