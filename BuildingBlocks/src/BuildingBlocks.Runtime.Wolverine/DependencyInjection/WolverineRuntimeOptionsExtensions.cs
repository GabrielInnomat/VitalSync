using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using Wolverine;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

public static class WolverineRuntimeOptionsExtensions
{
    public static BuildingBlocksOptions CustomizeWolverine(
        this BuildingBlocksOptions options,
        Action<WolverineOptions> customize)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(customize);

        options.Runtime.GetOrAdd(static () => new WolverineRuntimeActivator()).Customize(customize);
        return options;
    }
}
