using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using BuildingBlocks.Infrastructure.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ValidHandlersFixture;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class UnitOfWorkPresenceCheckTests
{
    [Fact]
    public async Task NoPersistenceAndScannedCommands_FailsNamingTheCommands()
    {
        using var provider = BuildProvider(options => options.AddHandlersFrom(typeof(RegistrationCommand).Assembly));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Check(provider).RunAsync(TestContext.Current.CancellationToken));

        Assert.Contains(nameof(RegistrationCommand), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(BuildingBlocksOptions.UseNoPersistence), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NoPersistenceAndNoScannedCommands_Passes()
    {
        using var provider = BuildProvider(_ => { });

        await Check(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HostRegisteredUnitOfWork_Passes()
    {
        using var provider = BuildProvider(
            options => options.AddHandlersFrom(typeof(RegistrationCommand).Assembly),
            services => services.AddScoped<IUnitOfWork, ProbeUnitOfWork>());

        await Check(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnitOfWorkRegisteredAfterBuildingBlocks_Passes()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddBuildingBlocks(options => options.AddHandlersFrom(typeof(RegistrationCommand).Assembly));
        services.AddScoped<IUnitOfWork, ProbeUnitOfWork>();

        using var provider = services.BuildServiceProvider();

        await Check(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UseNoPersistence_PassesAndLogsTheDeliberateChoice()
    {
        using var provider = BuildProvider(options => options
            .AddHandlersFrom(typeof(RegistrationCommand).Assembly)
            .UseNoPersistence());

        await Check(provider).RunAsync(TestContext.Current.CancellationToken);

        Assert.Contains(
            provider.GetRequiredService<FakeLogCollector>().GetSnapshot(),
            record => record.Level == LogLevel.Information
                && record.Message.Contains("UseNoPersistence", StringComparison.Ordinal));
    }

    [Fact]
    public void UseNoPersistence_CombinedWithAPersistenceStrategy_Throws()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddBuildingBlocks(options => options
            .UseNoPersistence()
            .UseMartenEventSourcing("Host=localhost;Database=probe;Username=test;Password=test")));

        Assert.Contains("UseNoPersistence", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UseNoPersistence_RegistersNoMessageStoreAndNeedsNoDomainEvents()
    {
        using var provider = BuildProvider(options => options.UseNoPersistence());

        Assert.False(provider.GetRequiredService<BuildingBlocksWiringSettings>().Persistence.IsSelected);
        Assert.False(provider.GetRequiredService<BuildingBlocksWiringSettings>().RequiresWolverine);
    }

    private static IStartupCheck Check(ServiceProvider provider) =>
        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is UnitOfWorkPresenceCheck);

    private static ServiceProvider BuildProvider(
        Action<BuildingBlocksOptions> configure,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        configureServices?.Invoke(services);
        services.AddBuildingBlocks(configure);
        return services.BuildServiceProvider();
    }

    private sealed class ProbeUnitOfWork : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
