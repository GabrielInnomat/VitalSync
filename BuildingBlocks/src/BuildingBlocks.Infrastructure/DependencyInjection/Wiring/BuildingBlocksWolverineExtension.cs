using BuildingBlocks.Infrastructure.Persistence;
using Wolverine;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal sealed class BuildingBlocksWolverineExtension(BuildingBlocksWiringSettings settings) : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.UseSystemTextJsonForSerialization(EntityKeyJsonOptions.Apply);

        if (settings.Persistence.IsSelected)
        {
            options.ApplyBuildingBlocksIdempotencyWindow();
            options.ApplyBuildingBlocksMessageStorageProvisioning(settings.Provisioning.ProvisionsInfrastructure);
            options.ApplyBuildingBlocksDomainEventRouting();
        }

        if (settings.Messaging.Transport is { } messaging)
        {
            options.ApplyBuildingBlocksMessagingDefaults(messaging, settings.Provisioning.ProvisionsInfrastructure);

            if (settings.Messaging.Subscription is { } subscription)
            {
                options.ApplyBuildingBlocksSubscription(subscription, messaging.ExchangeName);
            }
        }
    }
}
