using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

public static class HostApplicationBuilderExtensions
{
    public static TBuilder AddBuildingBlocks<TBuilder>(
        this TBuilder builder,
        Action<BuildingBlocksOptions> configure,
        Action<WolverineOptions>? configureWolverine = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var wiring = ServiceCollectionExtensions.AddBuildingBlocksCore(builder.Services, configure);

        if (!wiring.RequiresWolverine && configureWolverine is null)
        {
            return builder;
        }

        builder.UseWolverine(options =>
        {
            if (wiring.Persistence.EfCoreWriteConnectionString is { } writeConnectionString)
            {
                options.PersistMessagesWithPostgresql(writeConnectionString);
                options.UseEntityFrameworkCoreTransactions();
            }

            configureWolverine?.Invoke(options);
        });

        return builder;
    }
}
