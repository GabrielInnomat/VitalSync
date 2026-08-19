using System.Diagnostics.CodeAnalysis;
using GaWeCodes.Core;
using GaWeCodes.Core.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Deliberate exception to the package/namespace rule. The composition entry points stay in the
// shared root namespace so a consumer's Program.cs reaches AddBuildingBlocks and every Use*
// call with one using -- the same reason AddConsole() lives in Microsoft.Extensions.Logging
// and not in Microsoft.Extensions.Logging.Console. Every other type in this package matches
// its package name, so IDE0130 is suppressed here and nowhere else.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace GaWeCodes;
#pragma warning restore IDE0130
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
