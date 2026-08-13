using BuildingBlocks.Infrastructure.DependencyInjection.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class PersistenceRegistrationContext(
    IServiceCollection services,
    Func<bool> provisionsInfrastructure,
    RuntimeActivation runtime)
{
    public IServiceCollection Services => services;

    public bool ProvisionsInfrastructure => provisionsInfrastructure();

    public TActivator UseRuntime<TActivator>(Func<TActivator> create)
        where TActivator : class, IRuntimeActivator =>
        runtime.GetOrAdd(create);
}
