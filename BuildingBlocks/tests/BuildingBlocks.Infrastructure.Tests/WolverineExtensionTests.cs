using System.Reflection;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Dispatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class WolverineExtensionTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";

    private static readonly Uri RabbitMqUri = new("amqp://guest:guest@localhost:5672");

    private static readonly MessagingSettings TestMessagingSettings =
        new(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName);

    private static readonly Assembly TestAssembly = typeof(WolverineExtensionTests).Assembly;

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
        Assert.False(settings.Persistence.IsSelected);
        Assert.Null(settings.Persistence.EfCoreWriteConnectionString);
        Assert.Null(settings.Messaging);
    }

    [Fact]
    public void EfCoreSelection_RequestsRoutingAndEfCoreOutbox()
    {
        using var provider = BuildProvider(options =>
            options.UseEfCorePersistence<TestDbContext>(ConnectionString));

        var settings = provider.GetRequiredService<WolverineWiringSettings>();

        Assert.True(settings.Persistence.IsSelected);
        Assert.Equal(ConnectionString, settings.Persistence.EfCoreWriteConnectionString);
        Assert.Null(settings.Messaging);
    }

    [Fact]
    public void MartenSelection_RequestsRoutingWithoutEfCoreOutbox()
    {
        using var provider = BuildProvider(options =>
            options.UseMartenEventSourcing(ConnectionString));

        var settings = provider.GetRequiredService<WolverineWiringSettings>();

        Assert.True(settings.Persistence.IsSelected);
        Assert.Null(settings.Persistence.EfCoreWriteConnectionString);
        Assert.Null(settings.Messaging);
    }

    [Fact]
    public void MessagingSelection_RecordsTheBrokerUri()
    {
        using var provider = BuildProvider(options => options
            .UseMartenEventSourcing(ConnectionString)
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName));

        var settings = provider.GetRequiredService<WolverineWiringSettings>();

        Assert.Equal(RabbitMqUri, settings.Messaging!.RabbitMqUri);
        Assert.True(settings.RequiresWolverine);
    }

    [Fact]
    public void MessagingWithoutAPersistenceStrategy_FailsAtCompositionTime()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(options => options.UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)));

        Assert.Contains("durable", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("UseMartenEventSourcing", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MessagingAfterAPersistenceStrategy_IsAcceptedRegardlessOfCallOrder()
    {
        using var provider = BuildProvider(options => options
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)
            .UseMartenEventSourcing(ConnectionString));

        Assert.True(provider.GetRequiredService<WolverineWiringSettings>().Persistence.IsSelected);
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
        var options = ConfigureOptions(Settings(settings => settings.SelectPersistence(PersistenceChoice.Marten)));

        var endpoints = options.Transports.SelectMany(transport => transport.Endpoints());

        Assert.Contains(
            endpoints,
            endpoint => endpoint.Uri.ToString().Contains("building-blocks-domain-events", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Configure_WithDomainEventRouting_LetsAHandlerDependOnISender()
    {
        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddScoped<ISender, RequestSender>())
            .UseWolverine(options =>
            {
                new BuildingBlocksWolverineExtension(
                    Settings(settings => settings.SelectPersistence(PersistenceChoice.Marten)))
                    .Configure(options);
                options.Discovery.IncludeAssembly(typeof(WolverineExtensionTests).Assembly);
            })
            .StartAsync(TestContext.Current.CancellationToken);

        await host.Services.GetRequiredService<IMessageBus>()
            .InvokeAsync(new SenderDependentProbe(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Configure_WithBrokerUri_AddsTheRabbitMqTransport()
    {
        var options = ConfigureOptions(Settings(settings => settings.SelectMessaging(TestMessagingSettings)));

        Assert.Contains(options.Transports, transport => transport.Protocol == "rabbitmq");
    }

    [Fact]
    public void Configure_WithBrokerUri_DeclaresThePlatformExchangeAsDurable()
    {
        var options = ConfigureOptions(Settings(settings => settings.SelectMessaging(TestMessagingSettings)));

        var exchange = RabbitMqTransportOf(options)
            .Exchanges[TestMessaging.ExchangeName];

        Assert.True(exchange.IsDurable);
    }

    [Fact]
    public void Configure_WithSubscription_DeclaresTheQueueAsDurable()
    {
        var options = ConfigureOptions(Settings(settings =>
        {
            settings.SelectMessaging(TestMessagingSettings);
            settings.SelectSubscription(
                new IntegrationEventSubscription("fitness.integration-events", ["nutrition.*"], TestAssembly));
        }));

        var queue = RabbitMqTransportOf(options).Queues["fitness.integration-events"];

        Assert.True(queue.IsDurable);
    }

    [Fact]
    public void Configure_WithBrokerUri_EnablesPublisherConfirmationsAndTheirTracking()
    {
        var options = ConfigureOptions(Settings(settings => settings.SelectMessaging(TestMessagingSettings)));

        var channel = new WolverineRabbitMqChannelOptions();

        Assert.False(channel.PublisherConfirmationsEnabled);
        Assert.False(channel.PublisherConfirmationTrackingEnabled);

        var configureChannel = RabbitMqTransportOf(options).ChannelCreationOptions;

        Assert.NotNull(configureChannel);
        configureChannel(channel);

        Assert.True(channel.PublisherConfirmationsEnabled);
        Assert.True(channel.PublisherConfirmationTrackingEnabled);
    }

    private static RabbitMqTransport RabbitMqTransportOf(WolverineOptions options)
        => options.Transports.OfType<RabbitMqTransport>().Single();

    [Fact]
    public void SubscriptionSelection_RecordsQueueBindingsAndConsumerAssembly()
    {
        using var provider = BuildProvider(options => options
            .UseMartenEventSourcing(ConnectionString)
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)
            .SubscribeToIntegrationEvents("fitness.integration-events", TestAssembly, "nutrition.*", "analytics.*"));

        var subscription = provider.GetRequiredService<WolverineWiringSettings>().Subscription;

        Assert.NotNull(subscription);
        Assert.Equal("fitness.integration-events", subscription!.QueueName);
        Assert.Equal(["nutrition.*", "analytics.*"], subscription.TopicPatterns);
        Assert.Equal(TestAssembly, subscription.ConsumerAssembly);
    }

    [Fact]
    public void Subscription_WithoutMessaging_FailsAtCompositionTime()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(options =>
                options.SubscribeToIntegrationEvents("fitness.integration-events", TestAssembly, "nutrition.*")));

        Assert.Contains("UseWolverineMessaging", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Subscription_CalledTwice_Throws()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(options => options
                .UseMartenEventSourcing(ConnectionString)
                .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)
                .SubscribeToIntegrationEvents("first", TestAssembly, "nutrition.*")
                .SubscribeToIntegrationEvents("second", TestAssembly, "fitness.*")));

        Assert.Contains("one queue", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Subscription_WithNoTopicPattern_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BuildProvider(options => options
                .UseMartenEventSourcing(ConnectionString)
                .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)
                .SubscribeToIntegrationEvents("fitness.integration-events", TestAssembly)));
    }

    [Fact]
    public void Subscription_WithABlankTopicPattern_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BuildProvider(options => options
                .UseMartenEventSourcing(ConnectionString)
                .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)
                .SubscribeToIntegrationEvents("fitness.integration-events", TestAssembly, "  ")));
    }

    [Fact]
    public void Configure_WithSubscription_ListensOnTheQueue()
    {
        var options = ConfigureOptions(Settings(settings =>
        {
            settings.SelectMessaging(TestMessagingSettings);
            settings.SelectSubscription(
                new IntegrationEventSubscription("fitness.integration-events", ["nutrition.*"], TestAssembly));
        }));

        Assert.Contains(
            options.Transports.SelectMany(transport => transport.Endpoints()),
            endpoint => endpoint.Uri.ToString().Contains("fitness.integration-events", StringComparison.Ordinal));
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

    [Fact]
    public void Configure_WithPersistence_WidensTheInboxIdempotencyWindow()
    {
        var options = ConfigureOptions(Settings(settings =>
            settings.SelectPersistence(PersistenceChoice.Marten)));

        Assert.Equal(TimeSpan.FromDays(7), options.Durability.KeepAfterMessageHandling);
    }

    [Fact]
    public void TheInboxIdempotencyWindow_IsNotTheWolverineDefault()
    {
        Assert.NotEqual(
            new DurabilitySettings().KeepAfterMessageHandling,
            DependencyInjection.Wiring.WolverineOptionsExtensions.IdempotencyWindow);
    }

    [Fact]
    public void Configure_WithoutPersistence_LeavesTheIdempotencyWindowAlone()
    {
        var options = ConfigureOptions(new WolverineWiringSettings());

        Assert.Equal(
            new DurabilitySettings().KeepAfterMessageHandling,
            options.Durability.KeepAfterMessageHandling);
    }

    private static WolverineWiringSettings Settings(Action<WolverineWiringSettings> configure)
    {
        var settings = new WolverineWiringSettings();
        configure(settings);
        return settings;
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
        services.AddBuildingBlocks(options =>
        {
            options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
            configure(options);
        });
        return services.BuildServiceProvider();
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}

public sealed record SenderDependentProbe;

public sealed class SenderDependentProbeHandler
{
    public static Task HandleAsync(SenderDependentProbe probe, ISender sender) => Task.CompletedTask;
}

