using GaWeCodes.Core.Startup;
using GaWeCodes.Persistence.EfCore.StateStored;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GaWeCodes.Tests;

public sealed class WriteDbContextLifetimeCheckTests
{
    private const string UnusedConnectionString =
        "Host=localhost;Port=5432;Database=write_context_lifetime;Username=none;Password=none";

    [Fact]
    public async Task ATransientWriteContext_FailsTheStartWithTheReason()
    {
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RunCheckAsync(ServiceLifetime.Transient));

        Assert.Contains(nameof(FlushProbeContext), thrown.Message, StringComparison.Ordinal);
        Assert.Contains("Transient", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("writes no row at all", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ASingletonWriteContext_FailsTheStartWithItsOwnReason()
    {
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RunCheckAsync(ServiceLifetime.Singleton));

        Assert.Contains("Singleton", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("change tracker", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AScopedWriteContext_PassesTheStart() => await RunCheckAsync(ServiceLifetime.Scoped);

    [Fact]
    public async Task AWriteContextLeftToUseEfCorePersistence_PassesTheStart() => await RunCheckAsync(null);

    private static async Task RunCheckAsync(ServiceLifetime? preRegisteredLifetime)
    {
        var builder = Host.CreateApplicationBuilder();

        if (preRegisteredLifetime is { } lifetime)
        {
            builder.Services.AddDbContext<FlushProbeContext>(
                options => options.UseNpgsql(UnusedConnectionString),
                lifetime);
        }

        builder.AddBuildingBlocks(options => options
            .AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly)
            .UseEfCorePersistence<FlushProbeContext>(UnusedConnectionString));

        using var host = builder.Build();

        var check = host.Services.GetServices<IStartupCheck>()
            .OfType<WriteDbContextLifetimeCheck<FlushProbeContext>>()
            .Single();

        await check.RunAsync(TestContext.Current.CancellationToken);
    }
}
