using BuildingBlocks.Application;
using Wolverine;
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
/// <see cref="ApplyBuildingBlockMessagingDefaults"/> when integration events are published to RabbitMQ.
/// </remarks>
internal static class WolverineOptionsExtensions
{
    /// <summary>
    /// The name of the local, durable, strictly sequential queue every domain event is routed through.
    /// </summary>
    /// <remarks>
    /// Exposed so tests can address the queue this package configures instead of restating its name, which would
    /// let the two drift apart silently.
    /// </remarks>
    public const string DomainEventLocalQueueName = "building-blocks-domain-events";

    /// <summary>
    /// The name of the RabbitMQ topic exchange every integration event is published to.
    /// </summary>
    /// <remarks>
    /// One exchange for the whole platform (ADR-0023): consumers bind their own queue with a topic pattern
    /// (<c>nutrition.*</c>) instead of every context owning an exchange, so adding a subscriber never touches the
    /// publisher. Provisioned automatically by <c>AutoProvision</c>.
    /// </remarks>
    public const string IntegrationEventExchangeName = "vitalsync.integration-events";

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

        // The handler's dependencies are internal by design, which forces Wolverine's codegen into service
        // location for exactly these registrations. Opt them in explicitly so the safe default
        // (ServiceLocationPolicy.NotAllowed) stays intact for everything else instead of failing the first
        // delivered domain event with an InvalidServiceLocationException.
        options.CodeGeneration.AlwaysUseServiceLocationFor<IDomainEventPublisher>();
        options.CodeGeneration.AlwaysUseServiceLocationFor<IIntegrationEventSinkFactory>();

        options.PublishMessage<DomainEventEnvelope>()
            .ToLocalQueue(DomainEventLocalQueueName)
            .Sequential()
            .UseDurableInbox();

        return options;
    }

    /// <summary>
    /// Applies the default RabbitMQ transport, integration-event routing, retry, and dead-letter configuration.
    /// </summary>
    /// <remarks>
    /// Connecting the transport is not enough to move anything: without a routing rule Wolverine finds no subscriber
    /// for an integration event and <c>PublishAsync</c> silently drops it. The rule therefore matches on the
    /// <see cref="IIntegrationEvent"/> marker rather than on all messages — <see cref="DomainEventEnvelope"/> does not
    /// implement it and so cannot be matched onto the broker, which would leak a context's domain events across the
    /// boundary that ADR-0022 draws. The topic (routing key) of each event comes from its
    /// <c>[Topic("&lt;context&gt;.&lt;event&gt;")]</c> attribute, keeping the broker contract independent of the CLR
    /// namespace; consumers bind a queue with a topic pattern. Only required for hosts that select
    /// <c>UseWolverineMessaging</c>; a service with purely in-context projections needs only
    /// <see cref="ApplyBuildingBlockDomainEventRouting"/>.
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

        options.Publish(publishing => publishing
            .MessagesImplementing<IIntegrationEvent>()
            .ToRabbitTopics(IntegrationEventExchangeName));

        options.Policies.OnException<Exception>()
            .RetryWithCooldown(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromSeconds(2))
            .Then.MoveToErrorQueue();

        return options;
    }

}
