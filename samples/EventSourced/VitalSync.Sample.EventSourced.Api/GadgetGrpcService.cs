using GaWeCodes.Application.Cqrs;
using Grpc.Core;
using ProtoBuf.Grpc;
using static VitalSync.Sample.EventSourced.Api.FailureStatusMapping;
using VitalSync.Sample.EventSourced.Application;
using VitalSync.Sample.EventSourced.Contracts;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Api;

internal sealed class GadgetGrpcService(ISender sender) : IGadgetService
{
    public async ValueTask<CreateGadgetReply> CreateAsync(CreateGadgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sender.SendAsync(new CreateGadget(request.Name), context.CancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? new CreateGadgetReply { GadgetId = result.Value.Value.ToString() }
            : throw ToRpcException(result);
    }

    public async ValueTask<RenameGadgetReply> RenameAsync(RenameGadgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RenameGadget(ParseId(request.GadgetId), request.Name);
        var result = await sender.SendAsync(command, context.CancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? new RenameGadgetReply() : throw ToRpcException(result);
    }

    public async ValueTask<RetireGadgetReply> RetireAsync(RetireGadgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RetireGadget(ParseId(request.GadgetId), request.Reason);
        var result = await sender.SendAsync(command, context.CancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? new RetireGadgetReply() : throw ToRpcException(result);
    }

    public async ValueTask<GadgetReply> GetAsync(GetGadgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new GetGadget(ParseId(request.GadgetId));
        var result = await sender.SendAsync(query, context.CancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            throw ToRpcException(result);
        }

        var view = result.Value;
        return new GadgetReply
        {
            GadgetId = view.Id.ToString(),
            Name = view.Name,
            RenameCount = view.RenameCount,
            IsRetired = view.IsRetired,
        };
    }

    private static GadgetId ParseId(string value) =>
        Guid.TryParse(value, out var id)
            ? new GadgetId(id)
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{value}' is not a valid gadget id."));
}
