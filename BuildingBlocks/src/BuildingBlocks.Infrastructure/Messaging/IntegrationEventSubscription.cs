using System.Reflection;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// The subscribing half of a service's integration-event wiring: its queue, what it binds, and where its consumers live.
/// </summary>
/// <remarks>
/// Populated by <c>BuildingBlocksOptions.SubscribeToIntegrationEvents</c> and applied by
/// <see cref="BuildingBlocksWolverineExtension"/> when Wolverine bootstraps, mirroring how the publishing half is
/// recorded in <see cref="WolverineWiringSettings"/>. Keeping all three parts in one object is deliberate: a queue
/// without a binding receives nothing, a binding without discovered consumers discards what it receives, and neither
/// failure raises an error.
/// </remarks>
/// <param name="QueueName">The name of the queue this service listens on; owned by the subscriber, unknown to publishers.</param>
/// <param name="TopicPatterns">The routing-key patterns bound to the platform exchange, for example <c>nutrition.*</c>.</param>
/// <param name="ConsumerAssembly">The assembly holding the service's Wolverine consumers.</param>
internal sealed record IntegrationEventSubscription(
    string QueueName,
    IReadOnlyList<string> TopicPatterns,
    Assembly ConsumerAssembly);
