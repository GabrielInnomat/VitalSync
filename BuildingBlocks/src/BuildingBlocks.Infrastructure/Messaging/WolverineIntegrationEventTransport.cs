using BuildingBlocks.Application;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Wolverine-backed integration-event transport that publishes to RabbitMQ (ADR-0023).
/// </summary>
/// <remarks>
/// Wolverine is used strictly as the transport — never as the in-process mediator (ADR-0015/0023): the publisher hands
/// each mapped integration event to Wolverine's <see cref="IMessageBus"/>, which delivers it to the RabbitMQ broker
/// with the retry and dead-letter policies configured on the host. Requires the host to run Wolverine, typically via
/// <see cref="WolverineOptionsExtensions.ApplyBuildingBlockMessagingDefaults"/>.
/// </remarks>
/// <param name="messageBus">The Wolverine message bus resolved from the current scope.</param>
public sealed class WolverineIntegrationEventTransport(IMessageBus messageBus) : IIntegrationEventTransport
{
    /// <inheritdoc/>
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return messageBus.PublishAsync(integrationEvent).AsTask();
    }
}
