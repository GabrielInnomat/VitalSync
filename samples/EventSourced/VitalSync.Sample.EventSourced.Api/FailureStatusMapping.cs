using BuildingBlocks.Application.Results;
using Grpc.Core;

namespace VitalSync.Sample.EventSourced.Api;

internal static class FailureStatusMapping
{
    public static StatusCode ToStatusCode(FailureCategory category) => category switch
    {
        FailureCategory.Validation => StatusCode.InvalidArgument,
        FailureCategory.NotFound => StatusCode.NotFound,
        FailureCategory.Conflict => StatusCode.Aborted,
        FailureCategory.BusinessRule => StatusCode.FailedPrecondition,
        FailureCategory.Forbidden => StatusCode.PermissionDenied,
        _ => StatusCode.Unknown,
    };

    public static RpcException ToRpcException(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var failure = result.Failures[0];
        return new RpcException(
            new Status(ToStatusCode(failure.Category), $"{failure.Code}: {failure.Message}"));
    }
}
