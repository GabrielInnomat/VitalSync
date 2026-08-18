using GaWeCodes.DependencyInjection.Validation;
using GaWeCodes.Messaging.Transport;
using GaWeCodes.Persistence;
using GaWeCodes.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolverine;

namespace GaWeCodes.DependencyInjection.Wiring;

public static class WolverineRuntimeRegistration
{
    public static WolverineRuntimeActivator UseWolverineRuntime(this PersistenceRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        RegisterRuntimeServices(context.Services);
        return context.UseRuntime(static () => new WolverineRuntimeActivator());
    }

    public static WolverineRuntimeActivator UseWolverineRuntime(this MessagingTransportRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        RegisterRuntimeServices(context.Services);
        return context.UseRuntime(static () => new WolverineRuntimeActivator());
    }

    private static void RegisterRuntimeServices(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IWolverineExtension, BuildingBlocksWolverineExtension>());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupCheck, WolverineRuntimeCheck>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupCheck, InfrastructurePresenceCheck>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupCheck, IntegrationEventSubscriptionCheck>());
    }
}
