using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence;

internal sealed class PersistenceRegistrationContext(
    IServiceCollection services,
    Func<bool> provisionsInfrastructure,
    Action<IOutboxDurabilityConfigurator> addOutboxDurability)
{
    public IServiceCollection Services => services;

    public bool ProvisionsInfrastructure => provisionsInfrastructure();

    public void AddOutboxDurability(IOutboxDurabilityConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        addOutboxDurability(configurator);
    }
}
