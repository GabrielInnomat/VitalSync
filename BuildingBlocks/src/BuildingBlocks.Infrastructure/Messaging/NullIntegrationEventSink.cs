using BuildingBlocks.Application;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// No-op integration-event sink for hosts that have not enabled messaging.
/// </summary>
/// <remarks>
/// Registered (via its factory) as the fallback so the outbox-backed publisher works in hosts (and tests) without a
/// broker; every discarded event is logged at warning level, making an accidentally missing
/// <c>UseWolverineMessaging</c> call visible instead of silent.
/// </remarks>
/// <param name="logger">The logger that records discarded integration events.</param>
internal sealed class NullIntegrationEventSink(ILogger<NullIntegrationEventSink> logger) : IIntegrationEventSink
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

/// <summary>
/// Factory for the no-op sink, registered as the default until <c>UseWolverineMessaging</c> replaces it.
/// </summary>
/// <remarks>
/// Ignores the message context — there is nothing to bind when no broker is configured — and reuses a single sink
/// instance for all messages.
/// </remarks>
/// <param name="logger">The logger passed to the shared no-op sink.</param>
internal sealed class NullIntegrationEventSinkFactory(ILogger<NullIntegrationEventSink> logger) : IIntegrationEventSinkFactory
{
    private readonly NullIntegrationEventSink _sink = new(logger);

    /// <inheritdoc/>
    public IIntegrationEventSink Create(IMessageContext context) => _sink;
}
