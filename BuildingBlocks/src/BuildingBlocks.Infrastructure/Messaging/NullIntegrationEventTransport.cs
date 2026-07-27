using BuildingBlocks.Application;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// No-op integration-event transport for hosts that have not enabled messaging.
/// </summary>
/// <remarks>
/// Registered as the fallback so the outbox-backed publisher works in hosts (and tests) without a broker; every
/// discarded event is logged at warning level, making an accidentally missing
/// <c>UseWolverineMessaging</c> call visible instead of silent.
/// </remarks>
/// <param name="logger">The logger that records discarded integration events.</param>
public sealed class NullIntegrationEventTransport(ILogger<NullIntegrationEventTransport> logger) : IIntegrationEventTransport
{
    private static readonly Action<ILogger, string, Exception?> EventDiscardedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, "IntegrationEventDiscarded"),
            "No messaging transport is configured; discarding integration event {IntegrationEventType}");

    /// <inheritdoc/>
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        EventDiscardedMessage(logger, integrationEvent.GetType().Name, null);
        return Task.CompletedTask;
    }
}
