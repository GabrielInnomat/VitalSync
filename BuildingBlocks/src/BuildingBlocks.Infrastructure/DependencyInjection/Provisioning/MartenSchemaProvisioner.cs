using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Provisioning;

internal sealed class MartenSchemaProvisioner(
    IServiceProvider serviceProvider,
    ProvisioningSelection provisioning) : IStartupCheck
{
    public StartupPhase Phase => StartupPhase.BeforeHostedServicesStart;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!provisioning.ProvisionsInfrastructure)
        {
            return;
        }

        if (serviceProvider.GetService<IDocumentStore>() is not { } store)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync().ConfigureAwait(false);
    }
}
