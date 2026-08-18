using System.Reflection;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Dispatching;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using BuildingBlocks.Infrastructure.Persistence.StateStored;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;
using BuildingBlocksWiring = BuildingBlocks.Infrastructure.DependencyInjection.Wiring.WolverineOptionsExtensions;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class WolverineExtensionTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";

    private static readonly Uri RabbitMqUri = new("amqp://guest:guest@localhost:5672");

    private static readonly RabbitMqTransportAdapter TestMessagingSettings =
        new(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName);

    private static readonly Assembly TestAssembly = typeof(WolverineExtensionTests).Assembly;

    [Fact]
    public void AddBuildingBlocks_WithAPersistenceStrategy_RegistersTheWolverineExtension()
    {
        using var provider = BuildProvider(options =>
            options.UseEfCorePersistence<TestDbContext>(ConnectionString));

        Assert.Single(
            provider.GetServices<IWolverineExtension>(),
            extension => extension is BuildingBlocksWolverineExtension);
    }

    [Fact]
    public void AddBuildingBlocks_WithoutAnyCapability_RegistersNoWolverineExtension()
    {
        using var provider = BuildProvider(_ => { });

        Assert.Empty(provider.GetServices<IWolverineExtension>());
    }

    [Fact]
    public void NoCapabilitySelected_RequiresNoWolverine()
    {
        using var provider = BuildProvider(_ => { });

        var settings = provider.GetRequiredService<BuildingBlocksWiringSettings>();

        Assert.False(settings.RequiresRuntime);
        Assert.False(settings.Persistence.IsSelected);
        Assert.Null(settings.Persistence.WriteConnectionString);
        Assert.Null(settings.Messaging.Transport);
    }

    [Fact]
    public void EfCoreSelection_RequestsRoutingAndEfCoreOutbox()
    {
        using var provider = BuildProvider(options =>
            options.UseEfCorePersistence<TestDbContext>(ConnectionString));

        var settings = provider.GetRequiredService<BuildingBlocksWiringSettings>();

        Assert.True(settings.Persistence.IsSelected);
        Assert.Equal(ConnectionString, settings.Persistence.WriteConnectionString);
        Assert.Null(settings.Messaging.Transport);
    }

    [Fact]
    public void MartenSelection_RequestsRoutingWithoutEfCoreOutbox()
    {
        using var provider = BuildProvider(options =>
            options.UseMartenEventSourcing(ConnectionString));

        var settings = provider.GetRequiredService<BuildingBlocksWiringSettings>();

        Assert.True(settings.Persistence.IsSelected);
        Assert.Equal(ConnectionString, settings.Persistence.WriteConnectionString);
        Assert.Null(settings.Messaging.Transport);
    }

    [Fact]
    public void MessagingSelection_RecordsTheBrokerUri()
    {
        using var provider = BuildProvider(options => options
            .UseMartenEventSourcing(ConnectionString)
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName));

        var settings = provider.GetRequiredService<BuildingBlocksWiringSettings>();

        Assert.Equal(RabbitMqUri, ((RabbitMqTransportAdapter)settings.Messaging.Transport!).RabbitMqUri);
        Assert.True(settings.RequiresRuntime);
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

        Assert.True(provider.GetRequiredService<BuildingBlocksWiringSettings>().Persistence.IsSelected);
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
        var options = ConfigureOptions(Settings(settings => settings.Persistence.Select(PersistenceChoice.For(new MartenPersistenceAdapter(ConnectionString)))));

        var endpoints = options.Transports.SelectMany(transport => transport.Endpoints());

        Assert.Contains(
            endpoints,
            endpoint => endpoint.Uri.ToString().Contains("building-blocks-domain-events", StringComparison.Ordinal));
    }

    [Fact]
    public void PartitionKey_CombinesTheAggregateNameAndItsIdentity()
    {
        var envelope = new DomainEventEnvelope(
            "widget-created-v1",
            "{}",
            Guid.NewGuid(),
            "widget",
            "8f3a",
            1,
            DateTimeOffset.UtcNow);

        Assert.Equal("widget/8f3a", BuildingBlocksWiring.PartitionKeyFor(envelope));
    }

    [Fact]
    public async Task Configure_WithDomainEventRouting_LetsAHandlerDependOnISender()
    {
        using var host = await Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddScoped<ISender, RequestSender>())
            .UseWolverine(options =>
            {
                new BuildingBlocksWolverineExtension(
                    Settings(settings => settings.Persistence.Select(PersistenceChoice.For(new MartenPersistenceAdapter(ConnectionString)))))
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
        var options = ConfigureOptions(Settings(settings => settings.Messaging.SelectTransport(TestMessagingSettings)));

        Assert.Contains(options.Transports, transport => transport.Protocol == "rabbitmq");
    }

    [Fact]
    public void Configure_WithBrokerUri_DeclaresThePlatformExchangeAsDurable()
    {
        var options = ConfigureOptions(Settings(settings => settings.Messaging.SelectTransport(TestMessagingSettings)));

        var exchange = RabbitMqTransportOf(options)
            .Exchanges[TestMessaging.ExchangeName];

        Assert.True(exchange.IsDurable);
    }

    [Fact]
    public void Configure_WithSubscription_DeclaresTheQueueAsDurable()
    {
        var options = ConfigureOptions(Settings(settings =>
        {
            settings.Messaging.SelectTransport(TestMessagingSettings);
            settings.Messaging.SelectSubscription(
                new IntegrationEventSubscription("billing.integration-events", ["orders.*"], TestAssembly));
        }));

        var queue = RabbitMqTransportOf(options).Queues["billing.integration-events"];

        Assert.True(queue.IsDurable);
    }

    [Fact]
    public void Configure_WithBrokerUri_EnablesPublisherConfirmationsAndTheirTracking()
    {
        var options = ConfigureOptions(Settings(settings => settings.Messaging.SelectTransport(TestMessagingSettings)));

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
            .SubscribeToIntegrationEvents("billing.integration-events", TestAssembly, "orders.*", "reporting.*"));

        var subscription = provider.GetRequiredService<MessagingSelection>().Subscription;

        Assert.NotNull(subscription);
        Assert.Equal("billing.integration-events", subscription!.EndpointName);
        Assert.Equal(["orders.*", "reporting.*"], subscription.TopicPatterns);
        Assert.Equal(TestAssembly, subscription.ConsumerAssembly);
    }

    [Fact]
    public void Subscription_WithoutMessaging_FailsAtCompositionTime()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(options =>
                options.SubscribeToIntegrationEvents("billing.integration-events", TestAssembly, "orders.*")));

        Assert.Contains("without a messaging transport", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Subscription_CalledTwice_Throws()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(options => options
                .UseMartenEventSourcing(ConnectionString)
                .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)
                .SubscribeToIntegrationEvents("first", TestAssembly, "orders.*")
                .SubscribeToIntegrationEvents("second", TestAssembly, "billing.*")));

        Assert.Contains("one queue", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Subscription_WithNoTopicPattern_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BuildProvider(options => options
                .UseMartenEventSourcing(ConnectionString)
                .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)
                .SubscribeToIntegrationEvents("billing.integration-events", TestAssembly)));
    }

    [Fact]
    public void Subscription_WithABlankTopicPattern_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BuildProvider(options => options
                .UseMartenEventSourcing(ConnectionString)
                .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)
                .SubscribeToIntegrationEvents("billing.integration-events", TestAssembly, "  ")));
    }

    [Fact]
    public void Configure_WithSubscription_ListensOnTheQueue()
    {
        var options = ConfigureOptions(Settings(settings =>
        {
            settings.Messaging.SelectTransport(TestMessagingSettings);
            settings.Messaging.SelectSubscription(
                new IntegrationEventSubscription("billing.integration-events", ["orders.*"], TestAssembly));
        }));

        Assert.Contains(
            options.Transports.SelectMany(transport => transport.Endpoints()),
            endpoint => endpoint.Uri.ToString().Contains("billing.integration-events", StringComparison.Ordinal));
    }

    [Fact]
    public void Configure_WithNothingSelected_AddsNoRabbitMqTransportAndNoEnvelopeRoute()
    {
        var options = ConfigureOptions(new BuildingBlocksWiringSettings());

        Assert.DoesNotContain(options.Transports, transport => transport.Protocol == "rabbitmq");
        Assert.DoesNotContain(
            options.Transports.SelectMany(transport => transport.Endpoints()),
            endpoint => endpoint.Uri.ToString().Contains("building-blocks-domain-events", StringComparison.Ordinal));
    }

    [Fact]
    public void Configure_WithPersistence_WidensTheInboxIdempotencyWindow()
    {
        var options = ConfigureOptions(Settings(settings =>
            settings.Persistence.Select(PersistenceChoice.For(new MartenPersistenceAdapter(ConnectionString)))));

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
        var options = ConfigureOptions(new BuildingBlocksWiringSettings());

        Assert.Equal(
            new DurabilitySettings().KeepAfterMessageHandling,
            options.Durability.KeepAfterMessageHandling);
    }

    [Fact]
    public void Configure_WithoutABroker_StillAppliesTheFailurePolicies()
    {
        var options = ConfigureOptions(new BuildingBlocksWiringSettings());

        Assert.NotEmpty(options.Policies.Failures);
    }

    private static BuildingBlocksWiringSettings Settings(Action<BuildingBlocksWiringSettings> configure)
    {
        var settings = new BuildingBlocksWiringSettings();
        configure(settings);
        return settings;
    }

    private static WolverineOptions ConfigureOptions(BuildingBlocksWiringSettings settings)
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

