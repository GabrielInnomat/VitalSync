using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

internal sealed class BuildingBlocksWolverineExtension(WolverineWiringSettings settings) : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        if (settings.ApplyDomainEventRouting)
        {
            options.ApplyBuildingBlockDomainEventRouting();
        }

        if (settings.RabbitMqUri is { } rabbitMqUri)
        {
            options.ApplyBuildingBlockMessagingDefaults(rabbitMqUri);
        }

        if (settings.Subscription is { } subscription)
        {
            options.ApplyBuildingBlockSubscription(subscription);
        }
    }
}
