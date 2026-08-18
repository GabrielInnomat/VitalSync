using BuildingBlocks.Infrastructure.DependencyInjection.Extensibility;
using BuildingBlocks.Infrastructure.Messaging.Transport;
using BuildingBlocks.Infrastructure.Persistence;
using Wolverine;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal sealed class BuildingBlocksWolverineExtension(IWiringSnapshot wiring) : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.UseSystemTextJsonForSerialization(EntityKeyJsonOptions.Apply);

        if (wiring.PersistenceSelected)
        {
            options.ApplyBuildingBlocksIdempotencyWindow();
            options.ApplyBuildingBlocksMessageStorageProvisioning(wiring.ProvisionsInfrastructure);
            options.ApplyBuildingBlocksDomainEventRouting();
        }

        options.ApplyBuildingBlocksMessagingPolicies(wiring.IsTransientFault);

        if (wiring.Transport is { } transport)
        {
            if (transport is not IWolverineMessagingTransport wolverineTransport)
            {
                throw new InvalidOperationException(
                    $"The messaging transport {transport.Description} does not implement " +
                    $"{nameof(IWolverineMessagingTransport)}, so the Wolverine runtime cannot configure it. The " +
                    "host would start with a transport that is selected but never wired, and every integration " +
                    "event would be dropped silently. Implement the interface on the transport adapter or select " +
                    "a transport that targets this runtime.");
            }

            options.ApplyBuildingBlocksIntegrationEventTopics(wolverineTransport.ContextName);
            wolverineTransport.Configure(options, wiring.ProvisionsInfrastructure);

            if (wiring.Subscription is { } subscription)
            {
                options.ApplyBuildingBlocksSubscriptionDiscovery(subscription);
                wolverineTransport.ConfigureSubscription(options, subscription);
            }
        }
    }
}
