using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Persistence.Durability;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal sealed class InfrastructurePresenceCheck(
    IServiceProvider serviceProvider,
    BuildingBlocksWiringSettings settings) : IStartupCheck
{
    public StartupPhase Phase => StartupPhase.AfterHostedServicesStarted;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (settings.ProvisionsInfrastructure || !settings.Persistence.IsSelected)
        {
            return;
        }

        if (serviceProvider.GetService<IMessageStore>() is not { } messageStore)
        {
            return;
        }

        try
        {
            await messageStore.Admin.AssertStorageExistsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "This host does not provision infrastructure, but Wolverine's message storage is missing or does " +
                $"not match the configured schema in '{messageStore.Name}'. The outbox is what makes a commit and " +
                "its integration events one unit, so without those tables this host would accept " +
                "commands and lose every event they produce. Run the context's migration worker — the one host " +
                "that selects ProvisionInfrastructure(InfrastructureProvisioning.AtStartup) — before starting this " +
                "one.",
                exception);
        }
    }
}
