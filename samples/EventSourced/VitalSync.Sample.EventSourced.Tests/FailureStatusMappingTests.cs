using GaWeCodes.Application.Results;
using Grpc.Core;
using VitalSync.Sample.EventSourced.Api;

namespace VitalSync.Sample.EventSourced.Tests;

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
        var result = Result.Failed(Failure.NotFound("gadget.not_found", "No such gadget."));

        var exception = FailureStatusMapping.ToRpcException(result);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        Assert.Equal("gadget.not_found: No such gadget.", exception.Status.Detail);
    }

    [Fact]
    public void ToRpcException_WithSeveralFailures_CarriesEachOneInTheTrailers()
    {
        var result = Result.Failed(
        [
            new Failure("gadget.name.required", "The gadget name must not be empty.", FailureCategory.Validation)
            {
                Target = "name",
            },
            new Failure("gadget.component.label.required", "The label must not be empty.", FailureCategory.Validation)
            {
                Target = "label",
            },
        ]);

        var exception = FailureStatusMapping.ToRpcException(result);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Equal("2", exception.Trailers.GetValue(FailureTrailers.CountKey));
        Assert.Equal("gadget.name.required", exception.Trailers.GetValue("failure-0-code"));
        Assert.Equal("name", exception.Trailers.GetValue("failure-0-target"));
        Assert.Equal("label", exception.Trailers.GetValue("failure-1-target"));
    }

    [Fact]
    public void ToRpcException_WithoutATarget_OmitsTheTrailer()
    {
        var result = Result.Failed(Failure.BusinessRule("gadget.retired", "A retired gadget cannot change."));

        var exception = FailureStatusMapping.ToRpcException(result);

        Assert.Null(exception.Trailers.GetValue("failure-0-target"));
    }
}
