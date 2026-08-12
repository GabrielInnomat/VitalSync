using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Registration;

internal sealed class PersistenceRegistrar(
    IServiceCollection services,
    PersistenceSelection persistence,
    ProvisioningSelection provisioning)
{
    public void UseNone() => persistence.Select(PersistenceChoice.NoPersistence);

    public void Use(IPersistenceAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);

        persistence.Select(PersistenceChoice.For(adapter));
        adapter.Register(new PersistenceRegistrationContext(
            services,
            () => provisioning.ProvisionsInfrastructure,
            persistence.AddOutboxDurability));
    }
}
