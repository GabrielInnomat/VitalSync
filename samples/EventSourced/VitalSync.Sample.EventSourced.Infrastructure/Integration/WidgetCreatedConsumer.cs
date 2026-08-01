using BuildingBlocks.Application;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.EventSourced.Application;

namespace VitalSync.Sample.EventSourced.Infrastructure.Integration;

// The subscribing edge of the platform: a Wolverine message handler that does nothing but translate a
// contract from another bounded context into a command of this one. No domain logic, no repository, no
// database - the same shape as the gRPC adapter, for the same reason.
//
// The name is not free: Wolverine discovers handlers by convention from types ending in "Handler" or
// "Consumer", while CA1711 reserves the "EventHandler" suffix for .NET delegate types. "Consumer" is the one
// suffix that satisfies both. Discovery also does not reach this assembly by default; see the host wiring.
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

        // The Result/exception boundary flips here. Inside the service a failure is a value, and swallowing it
        // is safe because the caller sees it. On this edge nobody sees it: returning normally acknowledges the
        // message, so a failed command would be silently lost. Throwing is what hands the message back to
        // Wolverine's retry and dead-letter policy.
        if (!result.IsSuccess)
        {
            var failure = result.Failures[0];
            throw new InvalidOperationException(
                $"Mirroring widget '{message.WidgetId}' failed: {failure.Code}: {failure.Message}");
        }
    }
}
