using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Wolverine host configuration defaults for the Building Blocks messaging backbone.
/// </summary>
/// <remarks>
/// Every service host that persists through this package's unit-of-work implementations must run Wolverine
/// (ADR-0023), because domain events flow through Wolverine's own transactional outbox even when they never leave
/// the process (in-context projections, ADR-0022) — RabbitMQ is only needed for the subset of events selected as
/// integration events. Hosts never call these methods themselves: <see cref="BuildingBlocksWolverineExtension"/>
/// applies exactly the combination matching the host's capability selection when Wolverine bootstraps (ADR-0027) —
/// <see cref="ApplyBuildingBlockDomainEventRouting"/> whenever a persistence style was selected,
/// <see cref="ApplyBuildingBlockEfCoreOutbox"/> for state-stored contexts, and
/// <see cref="ApplyBuildingBlockMessagingDefaults"/> when integration events are published to RabbitMQ.
/// </remarks>
internal static class WolverineOptionsExtensions
{
    private const string DomainEventLocalQueueName = "building-blocks-domain-events";

    /// <summary>
    /// Applies the routing every host needs for domain events to flow through Wolverine's transactional outbox.
    /// </summary>
    /// <remarks>
    /// Wolverine only dispatches to handlers registered for a message's exact concrete type (it does not support
    /// interface- or base-type routing), so every domain event is wrapped in the single concrete
    /// <see cref="DomainEventEnvelope"/> before publishing (see the unit-of-work implementations in
    /// <c>BuildingBlocks.Infrastructure.Persistence</c>). This method makes this package's assembly discoverable so
    /// Wolverine finds <see cref="DomainEventEnvelopeHandler"/> regardless of which service hosts it, and routes the
    /// envelope to a durable, strictly sequential local queue so redelivery after a crash cannot reorder a single
    /// aggregate's events relative to one another (ADR-0022's per-aggregate ordering rule).
    /// </remarks>
    /// <param name="options">The Wolverine options being configured.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
    public static WolverineOptions ApplyBuildingBlockDomainEventRouting(this WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Discovery.IncludeAssembly(typeof(DomainEventEnvelopeHandler).Assembly);

        options.PublishMessage<DomainEventEnvelope>()
            .ToLocalQueue(DomainEventLocalQueueName)
            .Sequential()
            .UseDurableInbox();

        return options;
    }

    /// <summary>
    /// Applies the default RabbitMQ transport, retry, and dead-letter configuration.
    /// </summary>
    /// <remarks>
    /// Only required for hosts that also select <c>UseWolverineMessaging</c> to publish integration events; a service
    /// with purely in-context projections needs only <see cref="ApplyBuildingBlockDomainEventRouting"/>.
    /// </remarks>
    /// <param name="options">The Wolverine options being configured.</param>
    /// <param name="rabbitMqUri">The AMQP connection URI of the RabbitMQ broker (typically the Aspire-provided connection string).</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="rabbitMqUri"/> is <see langword="null"/>.</exception>
    public static WolverineOptions ApplyBuildingBlockMessagingDefaults(this WolverineOptions options, Uri rabbitMqUri)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(rabbitMqUri);

        options.UseRabbitMq(rabbitMqUri).AutoProvision();

        options.Policies.OnException<Exception>()
            .RetryWithCooldown(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromSeconds(2))
            .Then.MoveToErrorQueue();

        return options;
    }

    /// <summary>
    /// Activates Wolverine's EF Core transactional middleware, required for <c>IDbContextOutbox&lt;TContext&gt;</c> to
    /// enlist outgoing messages in the same transaction as a state-stored context's <c>SaveChanges</c>.
    /// </summary>
    /// <remarks>
    /// Only required for hosts that select <c>UseEfCorePersistence</c>; a purely event-sourced host needs only
    /// <see cref="ApplyBuildingBlockDomainEventRouting"/>.
    /// </remarks>
    /// <param name="options">The Wolverine options being configured.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
    public static WolverineOptions ApplyBuildingBlockEfCoreOutbox(this WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.UseEntityFrameworkCoreTransactions();

        return options;
    }
}
