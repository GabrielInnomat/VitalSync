using BuildingBlocks.Application;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;
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
    /// Activates Wolverine's EF Core transactional middleware and the options-side half of the PostgreSQL-backed
    /// durable message store, required for <c>IDbContextOutbox&lt;TContext&gt;</c> to enlist outgoing messages in the
    /// same transaction as a state-stored context's <c>SaveChanges</c>.
    /// </summary>
    /// <remarks>
    /// The EF Core outbox refuses to run without a database-backed Wolverine message store ("not using Database
    /// backed message persistence"). Its registration is split across the extension boundary: the service
    /// registrations happen at composition time in <see cref="EfCoreMessageStoreRegistration"/> (this extension runs
    /// after the provider is built, where they would be ineffective), while this method applies the parts that
    /// mutate the live <see cref="WolverineOptions"/> — codegen persistence strategies and error policies via
    /// <c>PersistMessagesWithPostgresql</c> (whose own duplicate service registrations are harmless no-ops here) and
    /// the EF Core transactional middleware. The store lives on the context's own write database, keeping outbox
    /// rows and aggregate state in the same database and transaction (ADR-0021/0022). Only required for hosts that
    /// select <c>UseEfCorePersistence</c>; a purely event-sourced host gets its message store from Marten's
    /// <c>IntegrateWithWolverine</c> and needs only <see cref="ApplyBuildingBlockDomainEventRouting"/>.
    /// </remarks>
    /// <param name="options">The Wolverine options being configured.</param>
    /// <param name="connectionString">The connection string of the context's write database, which hosts the durable message store.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="connectionString"/> is <see langword="null"/>.</exception>
    public static WolverineOptions ApplyBuildingBlockEfCoreOutbox(this WolverineOptions options, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionString);

        options.PersistMessagesWithPostgresql(connectionString);
        options.UseEntityFrameworkCoreTransactions();

        return options;
    }
}
