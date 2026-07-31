using BuildingBlocks.Application;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Creates the <see cref="IIntegrationEventSink"/> bound to the Wolverine message currently being handled.
/// </summary>
/// <remarks>
/// The sink must be created from the <see cref="IMessageContext"/> of the message being processed — never resolved
/// from the container — so that published integration events enroll in that message's outbox and inherit its
/// correlation (see <see cref="DomainEventEnvelopeHandler"/>, the only caller). Which sink is produced follows the
/// host's capability selection: <c>UseWolverineMessaging</c> selects the Wolverine-backed sink that publishes to
/// RabbitMQ (ADR-0023); without it, a warning no-op sink backs hosts that have not enabled messaging.
/// </remarks>
public interface IIntegrationEventSinkFactory
{
    /// <summary>
    /// Creates the sink bound to the specified message context.
    /// </summary>
    /// <param name="context">The Wolverine context of the message currently being handled.</param>
    /// <returns>The sink that publishes integration events within that message's transaction.</returns>
    IIntegrationEventSink Create(IMessageContext context);
}
