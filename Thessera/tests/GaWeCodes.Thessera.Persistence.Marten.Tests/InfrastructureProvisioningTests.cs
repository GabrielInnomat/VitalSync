using GaWeCodes.Thessera.Core.DependencyInjection;
using GaWeCodes.Thessera.Core.DependencyInjection.Wiring;
using GaWeCodes.Thessera.Core.Startup;
using GaWeCodes.Thessera.Messaging.RabbitMq;
using GaWeCodes.Thessera.Persistence.Marten;
using GaWeCodes.Thessera.Wolverine.DependencyInjection.Validation;
using GaWeCodes.Thessera.Wolverine.DependencyInjection.Wiring;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.RabbitMQ.Internal;

namespace GaWeCodes.Thessera.Tests;

public sealed class InfrastructureProvisioningTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test";

    private static readonly Uri RabbitMqUri = new("amqp://localhost:5672");

    private static readonly RabbitMqTransportAdapter TestMessagingSettings =
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
    public void TheBrokerTopologyCheck_IsNotRegisteredWithoutMessaging()
    {
        using var provider = BuildProvider(_ => { });

        Assert.DoesNotContain(provider.GetServices<IStartupCheck>(), check => check is BrokerTopologyCheck);
    }

    [Fact]
    public void TheBrokerTopologyCheck_IsRegisteredWhenMessagingIsSelected()
    {
        using var provider = BuildProvider(options => options
            .UseMartenEventStore(ConnectionString)
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName));

        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is BrokerTopologyCheck);
    }

    [Fact]
    public async Task TheBrokerTopologyCheck_PassesOnAProvisioningHost()
    {
        using var provider = BuildProvider(options => options
            .UseMartenEventStore(ConnectionString)
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName)
            .ProvisionInfrastructure(InfrastructureProvisioning.AtStartup));

        await BrokerCheck(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void WithoutProvisioning_MartenCreatesNoSchema()
    {
        using var provider = BuildProvider(options => options.UseMartenEventStore(ConnectionString));

        Assert.Equal(
            JasperFx.AutoCreate.None,
            AutoCreateOf(provider));
    }

    [Fact]
    public void WithProvisioning_MartenCreatesItsSchema()
    {
        using var provider = BuildProvider(options => options
            .UseMartenEventStore(ConnectionString)
            .ProvisionInfrastructure(InfrastructureProvisioning.AtStartup));

        Assert.Equal(
            JasperFx.AutoCreate.CreateOrUpdate,
            AutoCreateOf(provider));
    }

    [Fact]
    public void ThePresenceCheck_IsNotRegisteredWhenNothingNeedsIt()
    {
        using var provider = BuildProvider(_ => { });

        Assert.Empty(provider.GetServices<IStartupCheck>().OfType<InfrastructurePresenceCheck>());
    }

    [Fact]
    public void ThePresenceCheck_IsRegisteredAsSoonAsPersistenceIsSelected()
    {
        using var provider = BuildProvider(options => options.UseMartenEventStore(ConnectionString));

        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is InfrastructurePresenceCheck);
    }

    [Fact]
    public async Task ThePresenceCheck_PassesOnAProvisioningHost()
    {
        using var provider = BuildProvider(options => options
            .UseMartenEventStore(ConnectionString)
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

    private static WolverineOptions ConfigureOptions(ThesseraWiringSettings settings)
    {
        var options = new WolverineOptions();
        new ThesseraWolverineExtension(settings).Configure(options);
        return options;
    }

    private static ThesseraWiringSettings Settings(Action<ThesseraWiringSettings> configure)
    {
        var settings = new ThesseraWiringSettings();
        configure(settings);
        return settings;
    }

    private static ServiceProvider BuildProvider(Action<ThesseraOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddThessera(options =>
        {
            options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
            configure(options);
        });
        return services.BuildServiceProvider();
    }
}
