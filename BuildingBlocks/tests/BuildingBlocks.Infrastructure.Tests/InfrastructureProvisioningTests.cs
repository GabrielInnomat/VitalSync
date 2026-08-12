using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using BuildingBlocks.Infrastructure.Startup;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.RabbitMQ.Internal;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class InfrastructureProvisioningTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test";

    private static readonly Uri RabbitMqUri = new("amqp://localhost:5672");

    private static readonly MessagingSettings TestMessagingSettings =
        new(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName);

    [Fact]
    public void AHostThatSelectsNothing_ProvisionsNothing()
    {
        using var provider = BuildProvider(_ => { });

        Assert.False(provider.GetRequiredService<ProvisioningSelection>().ProvisionsInfrastructure);
    }

    [Fact]
    public void AHostThatSelectsAtStartup_ProvisionsInfrastructure()
    {
        using var provider = BuildProvider(options => options
            .ProvisionInfrastructure(InfrastructureProvisioning.AtStartup));

        Assert.True(provider.GetRequiredService<ProvisioningSelection>().ProvisionsInfrastructure);
    }

    [Fact]
    public void AnUndefinedProvisioningValue_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BuildProvider(options => options.ProvisionInfrastructure((InfrastructureProvisioning)42)));

    [Fact]
    public void WithoutProvisioning_TheMessageStorageIsNotBuiltAtStartup()
    {
        var options = ConfigureOptions(Settings(settings => settings.Persistence.Select(PersistenceChoice.For(new MartenPersistenceAdapter(ConnectionString)))));

        Assert.Equal(JasperFx.AutoCreate.None, options.AutoBuildMessageStorageOnStartup);
    }

    [Fact]
    public void WithProvisioning_TheMessageStorageIsBuiltAtStartup()
    {
        var options = ConfigureOptions(Settings(settings =>
        {
            settings.Persistence.Select(PersistenceChoice.For(new MartenPersistenceAdapter(ConnectionString)));
            settings.Provisioning.Select(InfrastructureProvisioning.AtStartup);
        }));

        Assert.Equal(JasperFx.AutoCreate.CreateOrUpdate, options.AutoBuildMessageStorageOnStartup);
    }

    [Fact]
    public void WithoutProvisioning_TheBrokerTopologyIsNotDeclaredAtAll()
    {
        var options = ConfigureOptions(Settings(settings => settings.Messaging.SelectTransport(TestMessagingSettings)));

        var transport = RabbitMqTransportOf(options);

        Assert.False(transport.AutoProvision);
        Assert.False(transport.Exchanges[TestMessaging.ExchangeName].DeclarePassive);
    }

    [Fact]
    public void WithProvisioning_TheBrokerTopologyIsCreated()
    {
        var options = ConfigureOptions(Settings(settings =>
        {
            settings.Messaging.SelectTransport(TestMessagingSettings);
            settings.Provisioning.Select(InfrastructureProvisioning.AtStartup);
        }));

        var transport = RabbitMqTransportOf(options);

        Assert.True(transport.AutoProvision);
        Assert.False(transport.Exchanges[TestMessaging.ExchangeName].DeclarePassive);
    }

    [Fact]
    public void TheBrokerTopologyCheck_IsRegisteredEvenWhenNothingNeedsIt()
    {
        using var provider = BuildProvider(_ => { });

        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is BrokerTopologyCheck);
    }

    [Fact]
    public async Task TheBrokerTopologyCheck_PassesWithoutMessaging()
    {
        using var provider = BuildProvider(_ => { });

        await BrokerCheck(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void WithoutProvisioning_MartenCreatesNoSchema()
    {
        using var provider = BuildProvider(options => options.UseMartenEventSourcing(ConnectionString));

        Assert.Equal(
            JasperFx.AutoCreate.None,
            AutoCreateOf(provider));
    }

    [Fact]
    public void WithProvisioning_MartenCreatesItsSchema()
    {
        using var provider = BuildProvider(options => options
            .UseMartenEventSourcing(ConnectionString)
            .ProvisionInfrastructure(InfrastructureProvisioning.AtStartup));

        Assert.Equal(
            JasperFx.AutoCreate.CreateOrUpdate,
            AutoCreateOf(provider));
    }

    [Fact]
    public void ThePresenceCheck_IsRegisteredEvenWhenNothingNeedsIt()
    {
        using var provider = BuildProvider(_ => { });

        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is InfrastructurePresenceCheck);
    }

    [Fact]
    public async Task ThePresenceCheck_PassesWithoutPersistence()
    {
        using var provider = BuildProvider(_ => { });

        await Check(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ThePresenceCheck_PassesOnAProvisioningHost()
    {
        using var provider = BuildProvider(options => options
            .UseMartenEventSourcing(ConnectionString)
            .ProvisionInfrastructure(InfrastructureProvisioning.AtStartup));

        await Check(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    private static JasperFx.AutoCreate AutoCreateOf(ServiceProvider provider) =>
        ((DocumentStore)provider.GetRequiredService<IDocumentStore>()).Options.AutoCreateSchemaObjects;

    private static IStartupCheck Check(ServiceProvider provider) =>
        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is InfrastructurePresenceCheck);

    private static IStartupCheck BrokerCheck(ServiceProvider provider) =>
        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is BrokerTopologyCheck);

    private static RabbitMqTransport RabbitMqTransportOf(WolverineOptions options)
        => options.Transports.OfType<RabbitMqTransport>().Single();

    private static WolverineOptions ConfigureOptions(BuildingBlocksWiringSettings settings)
    {
        var options = new WolverineOptions();
        new BuildingBlocksWolverineExtension(settings).Configure(options);
        return options;
    }

    private static BuildingBlocksWiringSettings Settings(Action<BuildingBlocksWiringSettings> configure)
    {
        var settings = new BuildingBlocksWiringSettings();
        configure(settings);
        return settings;
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
}
