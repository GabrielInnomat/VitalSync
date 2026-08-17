using System.Diagnostics.CodeAnalysis;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    [RequiresUnreferencedCode(TrimmingMessages.AssemblyScanning)]
    [RequiresDynamicCode(TrimmingMessages.DynamicGenerics)]
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services, Action<BuildingBlocksOptions> configure)
    {
        AddBuildingBlocksCore(services, configure);
        return services;
    }

    internal static BuildingBlocksWiringSettings AddBuildingBlocksCore(IServiceCollection services, Action<BuildingBlocksOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return BuildingBlocksComposition.Compose(services, configure);
    }
}
