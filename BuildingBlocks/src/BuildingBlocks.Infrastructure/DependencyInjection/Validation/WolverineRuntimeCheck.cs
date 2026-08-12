using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Startup;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Runtime;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal sealed class WolverineRuntimeCheck(
    IServiceProvider serviceProvider,
    BuildingBlocksWiringSettings settings) : SynchronousStartupCheck
{
    public override StartupPhase Phase => StartupPhase.BeforeHostedServicesStart;

    protected override void Run()
    {
        if (!settings.RequiresWolverine)
        {
            return;
        }

        if (serviceProvider.GetService<IWolverineRuntime>() is null)
        {
            throw new InvalidOperationException(
                "The selected Building Block capabilities (persistence and/or integration-event messaging) require " +
                "Wolverine, but no Wolverine runtime is registered. Register through the host-builder overload — " +
                "builder.AddBuildingBlocks(...) — which calls UseWolverine() and applies the Building Block " +
                "configuration itself. A host that deliberately wires Wolverine on top of the IServiceCollection " +
                "overload calls UseWolverine() on the host builder instead.");
        }
    }
}
