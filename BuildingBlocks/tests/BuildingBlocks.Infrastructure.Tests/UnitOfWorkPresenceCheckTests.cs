using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ValidHandlersFixture;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class UnitOfWorkPresenceCheckTests
{
    [Fact]
    public void NoPersistenceAndScannedCommands_FailsNamingTheCommands()
    {
        using var provider = BuildProvider(options => options.AddHandlersFrom(typeof(RegistrationCommand).Assembly));

        var exception = Assert.Throws<InvalidOperationException>(() => Check(provider).Run());

        Assert.Contains(nameof(RegistrationCommand), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(BuildingBlocksOptions.UseNoPersistence), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NoPersistenceAndNoScannedCommands_Passes()
    {
        using var provider = BuildProvider(_ => { });

        Check(provider).Run();
    }

    [Fact]
    public void HostRegisteredUnitOfWork_Passes()
    {
        using var provider = BuildProvider(
            options => options.AddHandlersFrom(typeof(RegistrationCommand).Assembly),
            services => services.AddScoped<IUnitOfWork, ProbeUnitOfWork>());

        Check(provider).Run();
    }

    [Fact]
    public void UnitOfWorkRegisteredAfterBuildingBlocks_Passes()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddBuildingBlocks(options => options.AddHandlersFrom(typeof(RegistrationCommand).Assembly));
        services.AddScoped<IUnitOfWork, ProbeUnitOfWork>();

        using var provider = services.BuildServiceProvider();

        Check(provider).Run();
    }

    [Fact]
    public void UseNoPersistence_PassesAndLogsTheDeliberateChoice()
    {
        using var provider = BuildProvider(options => options
            .AddHandlersFrom(typeof(RegistrationCommand).Assembly)
            .UseNoPersistence());

        Check(provider).Run();

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

        Assert.False(provider.GetRequiredService<WolverineWiringSettings>().Persistence.IsSelected);
        Assert.False(provider.GetRequiredService<WolverineWiringSettings>().RequiresWolverine);
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
