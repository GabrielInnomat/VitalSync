using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Wolverine extension that applies the Building Block defaults matching the host's capability selection.
/// </summary>
/// <remarks>
/// Registered by <c>AddBuildingBlocks</c> and picked up automatically when the host calls <c>UseWolverine</c>:
/// Wolverine applies every <see cref="IWolverineExtension"/> found in the container during bootstrapping. The
/// extension derives the required configuration from <see cref="WolverineWiringSettings"/> — domain-event routing
/// when a persistence style was selected, the EF Core transactional middleware for state-stored contexts, and the
/// RabbitMQ transport defaults when messaging was selected — so the host writes an empty <c>UseWolverine()</c> call
/// and cannot wire the outbox wrong (ADR-0027).
/// </remarks>
/// <param name="settings">The wiring flags populated by the host's Building Block selection.</param>
internal sealed class BuildingBlocksWolverineExtension(WolverineWiringSettings settings) : IWolverineExtension
{
    /// <inheritdoc/>
    public void Configure(WolverineOptions options)
    {
        if (settings.ApplyDomainEventRouting)
        {
            options.ApplyBuildingBlockDomainEventRouting();
        }

        // The EF Core outbox is deliberately absent here: both PersistMessagesWithPostgresql and
        // UseEntityFrameworkCoreTransactions modify the service collection, which Wolverine 3.0 forbids from a
        // container-registered extension. The host applies them via UseBuildingBlocksEfCorePersistence.

        if (settings.RabbitMqUri is { } rabbitMqUri)
        {
            options.ApplyBuildingBlockMessagingDefaults(rabbitMqUri);
        }

        // After the transport, never before: the subscription binds to it. AddBuildingBlocks has already rejected a
        // subscription without a broker URI, so reaching this line means the transport exists.
        if (settings.Subscription is { } subscription)
        {
            options.ApplyBuildingBlockSubscription(subscription);
        }
    }
}
