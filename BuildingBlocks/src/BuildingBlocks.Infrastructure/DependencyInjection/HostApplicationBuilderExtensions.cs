using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

public static class HostApplicationBuilderExtensions
{
    [RequiresUnreferencedCode(TrimmingMessages.AssemblyScanning)]
    [RequiresDynamicCode(TrimmingMessages.DynamicGenerics)]
    public static TBuilder AddBuildingBlocks<TBuilder>(
        this TBuilder builder,
        Action<BuildingBlocksOptions> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var wiring = ServiceCollectionExtensions.AddBuildingBlocksCore(builder.Services, configure);

        wiring.Runtime.Activator?.Activate(builder, wiring);

        return builder;
    }
}
