using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class WolverineExtensionTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";

    private static readonly Uri RabbitMqUri = new("amqp://guest:guest@localhost:5672");

    [Fact]
    public void AddBuildingBlocks_RegistersTheWolverineExtension()
    {
        using var provider = BuildProvider(_ => { });

        Assert.Single(
            provider.GetServices<IWolverineExtension>(),
            extension => extension is BuildingBlocksWolverineExtension);
    }

    [Fact]
    public void NoCapabilitySelected_RequiresNoWolverine()
    {
        using var provider = BuildProvider(_ => { });

        var settings = provider.GetRequiredService<WolverineWiringSettings>();

        Assert.False(settings.RequiresWolverine);
        Assert.False(settings.ApplyDomainEventRouting);
        Assert.False(settings.ApplyEfCoreOutbox);
        Assert.Null(settings.RabbitMqUri);
    }

    [Fact]
    public void EfCoreSelection_RequestsRoutingAndEfCoreOutbox()
    {
        using var provider = BuildProvider(options =>
            options.UseEfCorePersistence<TestDbContext>(ConnectionString));

        var settings = provider.GetRequiredService<WolverineWiringSettings>();

        Assert.True(settings.ApplyDomainEventRouting);
        Assert.True(settings.ApplyEfCoreOutbox);
        Assert.Null(settings.RabbitMqUri);
    }

    [Fact]
    public void MartenSelection_RequestsRoutingWithoutEfCoreOutbox()
    {
        using var provider = BuildProvider(options =>
            options.UseMartenEventSourcing(ConnectionString));

        var settings = provider.GetRequiredService<WolverineWiringSettings>();

        Assert.True(settings.ApplyDomainEventRouting);
        Assert.False(settings.ApplyEfCoreOutbox);
        Assert.Null(settings.RabbitMqUri);
    }

    [Fact]
    public void MessagingSelection_RecordsTheBrokerUri()
    {
        using var provider = BuildProvider(options =>
            options.UseWolverineMessaging(RabbitMqUri));

        var settings = provider.GetRequiredService<WolverineWiringSettings>();

        Assert.Equal(RabbitMqUri, settings.RabbitMqUri);
        Assert.True(settings.RequiresWolverine);
    }

    [Fact]
    public void EfCoreSelection_RegistersTheDbContext()
    {
        using var provider = BuildProvider(options =>
            options.UseEfCorePersistence<TestDbContext>(ConnectionString));

        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<TestDbContext>());
    }

    [Fact]
    public void Configure_WithDomainEventRouting_RoutesTheEnvelopeToTheLocalQueue()
    {
        var options = ConfigureOptions(new WolverineWiringSettings { ApplyDomainEventRouting = true });

        var endpoints = options.Transports.SelectMany(transport => transport.Endpoints());

        Assert.Contains(
            endpoints,
            endpoint => endpoint.Uri.ToString().Contains("building-blocks-domain-events", StringComparison.Ordinal));
    }

    [Fact]
    public void Configure_WithBrokerUri_AddsTheRabbitMqTransport()
    {
        var options = ConfigureOptions(new WolverineWiringSettings { RabbitMqUri = RabbitMqUri });

        Assert.Contains(options.Transports, transport => transport.Protocol == "rabbitmq");
    }

    [Fact]
    public void Configure_WithNothingSelected_AddsNoRabbitMqTransportAndNoEnvelopeRoute()
    {
        var options = ConfigureOptions(new WolverineWiringSettings());

        Assert.DoesNotContain(options.Transports, transport => transport.Protocol == "rabbitmq");
        Assert.DoesNotContain(
            options.Transports.SelectMany(transport => transport.Endpoints()),
            endpoint => endpoint.Uri.ToString().Contains("building-blocks-domain-events", StringComparison.Ordinal));
    }

    private static WolverineOptions ConfigureOptions(WolverineWiringSettings settings)
    {
        var options = new WolverineOptions();
        new BuildingBlocksWolverineExtension(settings).Configure(options);
        return options;
    }

    private static ServiceProvider BuildProvider(Action<BuildingBlocksOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocks(configure);
        return services.BuildServiceProvider();
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
