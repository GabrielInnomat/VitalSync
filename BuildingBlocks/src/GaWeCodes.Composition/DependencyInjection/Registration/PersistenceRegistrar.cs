using GaWeCodes.DependencyInjection.Extensibility;
using GaWeCodes.DependencyInjection.Wiring;
using GaWeCodes.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GaWeCodes.DependencyInjection.Registration;

internal sealed class PersistenceRegistrar(
    IServiceCollection services,
    PersistenceSelection persistence,
    ProvisioningSelection provisioning,
    RuntimeActivation runtime)
{
    public void UseNone() => persistence.Select(PersistenceChoice.NoPersistence);

    public void Use(IPersistenceAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);

        persistence.Select(PersistenceChoice.For(adapter));
        adapter.Register(new PersistenceRegistrationContext(
            services,
            () => provisioning.ProvisionsInfrastructure,
            runtime));
    }
}
