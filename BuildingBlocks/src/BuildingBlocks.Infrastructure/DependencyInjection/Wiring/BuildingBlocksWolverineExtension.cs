using BuildingBlocks.Infrastructure.Persistence;
using Wolverine;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal sealed class BuildingBlocksWolverineExtension(WolverineWiringSettings settings) : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.UseSystemTextJsonForSerialization(EntityKeyJsonOptions.Apply);

        if (settings.Persistence.IsSelected)
        {
            options.ApplyBuildingBlocksIdempotencyWindow();
            options.ApplyBuildingBlocksMessageStorageProvisioning(settings.ProvisionsInfrastructure);
            options.ApplyBuildingBlocksDomainEventRouting();
        }

        if (settings.Messaging is { } messaging)
        {
            options.ApplyBuildingBlocksMessagingDefaults(messaging, settings.ProvisionsInfrastructure);

            if (settings.Subscription is { } subscription)
            {
                options.ApplyBuildingBlocksSubscription(subscription, messaging.ExchangeName);
            }
        }
    }
}
