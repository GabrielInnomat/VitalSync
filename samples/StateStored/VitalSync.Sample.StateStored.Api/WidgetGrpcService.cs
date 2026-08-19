using GaWeCodes.Thessera.Application.Cqrs;
using Grpc.Core;
using ProtoBuf.Grpc;
using static VitalSync.Sample.StateStored.Api.FailureStatusMapping;
using VitalSync.Sample.StateStored.Application;
using VitalSync.Sample.StateStored.Contracts;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Api;

internal sealed class WidgetGrpcService(ISender sender) : IWidgetService
{
    public async ValueTask<CreateWidgetReply> CreateAsync(CreateWidgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sender.SendAsync(new CreateWidget(request.Name), context.CancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? new CreateWidgetReply { WidgetId = result.Value.Value.ToString() }
            : throw ToRpcException(result);
    }

    public async ValueTask<RenameWidgetReply> RenameAsync(RenameWidgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RenameWidget(ParseId(request.WidgetId), request.Name);
        var result = await sender.SendAsync(command, context.CancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? new RenameWidgetReply() : throw ToRpcException(result);
    }

    public async ValueTask<WidgetReply> GetAsync(GetWidgetRequest request, CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new GetWidget(ParseId(request.WidgetId));
        var result = await sender.SendAsync(query, context.CancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            throw ToRpcException(result);
        }

        var view = result.Value;
        return new WidgetReply
        {
            WidgetId = view.Id.ToString(),
            Name = view.Name,
            RenameCount = view.RenameCount,
            PartCount = view.PartCount,
            TotalQuantity = view.TotalQuantity,
        };
    }

    public async ValueTask<AddWidgetPartReply> AddPartAsync(
        AddWidgetPartRequest request,
        CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new AddWidgetPart(ParseId(request.WidgetId), request.Label, request.Quantity);
        var result = await sender.SendAsync(command, context.CancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? new AddWidgetPartReply { PartId = result.Value.Value.ToString() }
            : throw ToRpcException(result);
    }

    public async ValueTask<ChangeWidgetPartQuantityReply> ChangePartQuantityAsync(
        ChangeWidgetPartQuantityRequest request,
        CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new ChangeWidgetPartQuantity(
            ParseId(request.WidgetId),
            ParsePartId(request.PartId),
            request.Quantity);
        var result = await sender.SendAsync(command, context.CancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? new ChangeWidgetPartQuantityReply() : throw ToRpcException(result);
    }

    public async ValueTask<RemoveWidgetPartReply> RemovePartAsync(
        RemoveWidgetPartRequest request,
        CallContext context = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RemoveWidgetPart(ParseId(request.WidgetId), ParsePartId(request.PartId));
        var result = await sender.SendAsync(command, context.CancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? new RemoveWidgetPartReply { Label = result.Value }
            : throw ToRpcException(result);
    }

    private static WidgetPartId ParsePartId(string value) =>
        Guid.TryParse(value, out var id)
            ? new WidgetPartId(id)
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{value}' is not a valid part id."));

    private static WidgetId ParseId(string value) =>
        Guid.TryParse(value, out var id)
            ? new WidgetId(id)
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{value}' is not a valid widget id."));
}
