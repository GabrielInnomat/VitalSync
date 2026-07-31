using BuildingBlocks.Application;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Wolverine-backed integration-event sink that publishes to RabbitMQ through the handled message's context (ADR-0023).
/// </summary>
/// <remarks>
/// Wolverine is used strictly as the transport — never as the in-process mediator (ADR-0015/0023). Publishing through
/// the handler's <see cref="IMessageContext"/> (instead of a container-resolved bus) enrolls each integration event in
/// the outbox of the message being processed: it is only released to the broker after the handling succeeds — a failed
/// handler leaks nothing across the context boundary — and the originating correlation is propagated onto the outgoing
/// envelope. The RabbitMQ routing, retry, and dead-letter defaults are applied by
/// <see cref="BuildingBlocksWolverineExtension"/> when the host calls <c>UseWolverine</c>.
/// </remarks>
/// <param name="context">The Wolverine context of the message currently being handled.</param>
internal sealed class WolverineIntegrationEventSink(IMessageContext context) : IIntegrationEventSink
{
    /// <inheritdoc/>
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return context.PublishAsync(integrationEvent).AsTask();
    }
}

/// <summary>
/// Factory for the Wolverine-backed sink, selected by <c>UseWolverineMessaging</c>.
/// </summary>
/// <remarks>
/// Replaces the no-op default registration so integration events reach RabbitMQ; the sink itself is created per
/// handled message from the handler's <see cref="IMessageContext"/>.
/// </remarks>
internal sealed class WolverineIntegrationEventSinkFactory : IIntegrationEventSinkFactory
{
    /// <inheritdoc/>
    public IIntegrationEventSink Create(IMessageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new WolverineIntegrationEventSink(context);
    }
}
