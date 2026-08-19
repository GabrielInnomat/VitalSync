using GaWeCodes.Thessera.Core.DependencyInjection;
using GaWeCodes.Thessera.Core.DependencyInjection.Wiring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GaWeCodes.Thessera.Tests;

public sealed class PersistenceStrategySelectionTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";

    [Fact]
    public void AddThessera_WithEfCoreOnly_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddThessera(options => WithDomainEvents(options).UseEfCorePersistence<TestDbContext>(ConnectionString)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddThessera_WithMartenOnly_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddThessera(options => WithDomainEvents(options).UseMartenEventSourcing(ConnectionString)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddThessera_WithEfCoreSelectedTwice_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddThessera(options => WithDomainEvents(options)
                .UseEfCorePersistence<TestDbContext>(ConnectionString)
                .UseEfCorePersistence<TestDbContext>(ConnectionString)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddThessera_WithEfCoreSelectedTwiceUnderDifferentConnectionStrings_Throws()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddThessera(options => WithDomainEvents(options)
                .UseEfCorePersistence<TestDbContext>(ConnectionString)
                .UseEfCorePersistence<TestDbContext>("Host=elsewhere;Database=other;Username=test;******")));

        Assert.Contains("different databases", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddThessera_WithMartenSelectedTwice_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddThessera(options => WithDomainEvents(options)
                .UseMartenEventSourcing(ConnectionString)
                .UseMartenEventSourcing(ConnectionString)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddThessera_WithEfCoreThenMarten_Throws()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddThessera(options => options
                .UseEfCorePersistence<TestDbContext>(ConnectionString)
                .UseMartenEventSourcing(ConnectionString)));

        Assert.Contains("persistence strateg", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UseEfCorePersistence", exception.Message, StringComparison.Ordinal);
        Assert.Contains("UseMartenEventSourcing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddThessera_WithMartenThenEfCore_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddThessera(options => options
                .UseMartenEventSourcing(ConnectionString)
                .UseEfCorePersistence<TestDbContext>(ConnectionString)));
    }

    [Fact]
    public void AddThessera_CombinesSelectionsFromSeparateSatellitePackages()
    {
        var services = new ServiceCollection();

        services.AddThessera(options => WithDomainEvents(options)
            .UseEfCorePersistence<TestDbContext>(ConnectionString)
            .UseWolverineMessaging(
                new Uri("amqp://localhost:5672"),
                TestMessaging.ExchangeName,
                TestMessaging.ContextName));

        using var provider = services.BuildServiceProvider();
        var wiring = provider.GetRequiredService<ThesseraWiringSettings>();

        Assert.True(wiring.Persistence.IsSelected);
        Assert.True(wiring.Messaging.IsSelected);
    }

    private static ThesseraOptions WithDomainEvents(ThesseraOptions options) =>
        options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}

