using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using BuildingBlocks.Infrastructure.Persistence.StateStored;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class PersistenceStrategySelectionTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";

    [Fact]
    public void AddBuildingBlocks_WithEfCoreOnly_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddBuildingBlocks(options => WithDomainEvents(options).UseEfCorePersistence<TestDbContext>(ConnectionString)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddBuildingBlocks_WithMartenOnly_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddBuildingBlocks(options => WithDomainEvents(options).UseMartenEventSourcing(ConnectionString)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddBuildingBlocks_WithEfCoreSelectedTwice_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddBuildingBlocks(options => WithDomainEvents(options)
                .UseEfCorePersistence<TestDbContext>(ConnectionString)
                .UseEfCorePersistence<TestDbContext>(ConnectionString)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddBuildingBlocks_WithEfCoreSelectedTwiceUnderDifferentConnectionStrings_Throws()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBuildingBlocks(options => WithDomainEvents(options)
                .UseEfCorePersistence<TestDbContext>(ConnectionString)
                .UseEfCorePersistence<TestDbContext>("Host=elsewhere;Database=other;Username=test;******")));

        Assert.Contains("different databases", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBuildingBlocks_WithMartenSelectedTwice_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddBuildingBlocks(options => WithDomainEvents(options)
                .UseMartenEventSourcing(ConnectionString)
                .UseMartenEventSourcing(ConnectionString)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddBuildingBlocks_WithEfCoreThenMarten_Throws()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBuildingBlocks(options => options
                .UseEfCorePersistence<TestDbContext>(ConnectionString)
                .UseMartenEventSourcing(ConnectionString)));

        Assert.Contains("persistence strateg", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UseEfCorePersistence", exception.Message, StringComparison.Ordinal);
        Assert.Contains("UseMartenEventSourcing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBuildingBlocks_WithMartenThenEfCore_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddBuildingBlocks(options => options
                .UseMartenEventSourcing(ConnectionString)
                .UseEfCorePersistence<TestDbContext>(ConnectionString)));
    }

    [Fact]
    public void AddBuildingBlocks_CombinesSelectionsFromSeparateSatellitePackages()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocks(options => WithDomainEvents(options)
            .UseEfCorePersistence<TestDbContext>(ConnectionString)
            .UseWolverineMessaging(
                new Uri("amqp://localhost:5672"),
                TestMessaging.ExchangeName,
                TestMessaging.ContextName));

        using var provider = services.BuildServiceProvider();
        var wiring = provider.GetRequiredService<BuildingBlocksWiringSettings>();

        Assert.True(wiring.Persistence.IsSelected);
        Assert.True(wiring.Messaging.IsSelected);
    }

    private static BuildingBlocksOptions WithDomainEvents(BuildingBlocksOptions options) =>
        options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}

