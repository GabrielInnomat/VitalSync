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
            options.ApplyBuildingBlockIdempotencyWindow();
            options.ApplyBuildingBlockDomainEventRouting();
        }

        if (settings.Messaging is { } messaging)
        {
            options.ApplyBuildingBlockMessagingDefaults(messaging);

            if (settings.Subscription is { } subscription)
            {
                options.ApplyBuildingBlockSubscription(subscription, messaging.ExchangeName);
            }
        }
    }
}
