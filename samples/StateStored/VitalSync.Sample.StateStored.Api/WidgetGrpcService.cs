using BuildingBlocks.Application;
using Grpc.Core;
using ProtoBuf.Grpc;
using VitalSync.Sample.StateStored.Application;
using VitalSync.Sample.StateStored.Contracts;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Api;

// A thin adapter over ISender (ADR-0023 scope note): no business logic, no persistence, no direct use of
// repositories. Everything it does is translate transport shapes into commands and Result into gRPC status
// codes - and that translation belongs to the host, never to Application (see CLAUDE.md).
internal sealed class WidgetGrpcService(ISender sender) : IWidgetService
{
    public async ValueTask<CreateWidgetReply> CreateAsync(CreateWidgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sender.Send(new CreateWidget(request.Name), context.CancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? new CreateWidgetReply { WidgetId = result.Value.Value.ToString() }
            : throw ToRpcException(result);
    }

    public async ValueTask<RenameWidgetReply> RenameAsync(RenameWidgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RenameWidget(ParseId(request.WidgetId), request.Name);
        var result = await sender.Send(command, context.CancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? new RenameWidgetReply() : throw ToRpcException(result);
    }

    public async ValueTask<WidgetReply> GetAsync(GetWidgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new GetWidget(ParseId(request.WidgetId));
        var result = await sender.Send(query, context.CancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            throw ToRpcException(result);
        }

        var view = result.Value;
        return new WidgetReply { WidgetId = view.Id.ToString(), Name = view.Name, RenameCount = view.RenameCount };
    }

    private static WidgetId ParseId(string value) =>
        Guid.TryParse(value, out var id)
            ? new WidgetId(id)
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{value}' is not a valid widget id."));

    // FailureCategory carries the semantics; mapping it onto a transport is the host's job, which is why
    // Application never references gRPC.
    private static RpcException ToRpcException(Result result)
    {
        var failure = result.Failures[0];
        var status = failure.Category switch
        {
            FailureCategory.Validation => StatusCode.InvalidArgument,
            FailureCategory.NotFound => StatusCode.NotFound,
            FailureCategory.Conflict => StatusCode.Aborted,
            FailureCategory.BusinessRule => StatusCode.FailedPrecondition,
            _ => StatusCode.Unknown,
        };

        return new RpcException(new Status(status, $"{failure.Code}: {failure.Message}"));
    }
}
