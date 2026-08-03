using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine.Runtime;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

internal sealed class WolverineWiringStartupValidator(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Validate();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void Validate()
    {
        if (serviceProvider.GetService<IWolverineRuntime>() is null)
        {
            throw new InvalidOperationException(
                "The selected Building Block capabilities (persistence and/or integration-event messaging) require " +
                "Wolverine, but no Wolverine runtime is registered. Register through the host-builder overload — " +
                "builder.AddBuildingBlocks(...) — which calls UseWolverine() and applies the Building Block " +
                "configuration itself. A host that deliberately wires Wolverine on top of the IServiceCollection " +
                "overload calls UseWolverine() on the host builder instead. To run without this check, set " +
                "BuildingBlocksOptions.ValidateWolverineOnStart to false.");
        }
    }
}
