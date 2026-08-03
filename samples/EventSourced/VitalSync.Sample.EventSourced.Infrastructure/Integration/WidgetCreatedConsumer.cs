using BuildingBlocks.Application;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.EventSourced.Application;

namespace VitalSync.Sample.EventSourced.Infrastructure.Integration;

public sealed class WidgetCreatedConsumer
{
    public static async Task Handle(
        WidgetCreatedIntegrationEvent message,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sender);

        var result = await sender.Send(new MirrorWidget(message.WidgetId, message.Name), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var failure = result.Failures[0];
            throw new InvalidOperationException(
                $"Mirroring widget '{message.WidgetId}' failed: {failure.Code}: {failure.Message}");
        }
    }
}
