using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal sealed partial class UnitOfWorkPresenceCheck(
    IServiceProvider serviceProvider,
    ILogger<UnitOfWorkPresenceCheck> logger) : IStartupCheck
{
    public StartupPhase Phase => StartupPhase.BeforeHostedServicesStart;

    public void Run()
    {
        if (serviceProvider.GetService<IServiceProviderIsService>() is { } probe
            && probe.IsService(typeof(IUnitOfWork)))
        {
            return;
        }

        LogNoPersistenceConfigured(logger);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No persistence configured — commands are dispatched without a unit of work and nothing is committed. This is intended only for tests, gateway services, and hosts with their own persistence.")]
    private static partial void LogNoPersistenceConfigured(ILogger logger);
}
