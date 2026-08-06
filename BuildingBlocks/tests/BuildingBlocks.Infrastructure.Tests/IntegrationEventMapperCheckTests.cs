using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ValidHandlersFixture;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class IntegrationEventMapperCheckTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test";

    private static readonly Uri RabbitMqUri = new("amqp://localhost:5672");

    [Fact]
    public void MapperWithoutATransport_FailsNamingTheMapper()
    {
        using var provider = BuildProvider(options => options.AddHandlersFrom(typeof(RegistrationMapper).Assembly));

        var exception = Assert.Throws<InvalidOperationException>(() => Check(provider).Run());

        Assert.Contains(nameof(RegistrationMapper), exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            nameof(BuildingBlocksOptions.UseWolverineMessaging),
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void NoMapper_Passes()
    {
        using var provider = BuildProvider(_ => { });

        Check(provider).Run();
    }

    [Fact]
    public void MapperWithATransport_Passes()
    {
        using var provider = BuildProvider(options => options
            .AddHandlersFrom(typeof(RegistrationMapper).Assembly)
            .UseMartenEventSourcing(ConnectionString)
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName));

        Check(provider).Run();
    }

    [Fact]
    public void MapperAndAHostSuppliedSinkFactory_Passes()
    {
        using var provider = BuildProvider(
            options => options.AddHandlersFrom(typeof(RegistrationMapper).Assembly),
            services => services.Replace(ServiceDescriptor.Singleton<IIntegrationEventSinkFactory>(
                new WolverineIntegrationEventSinkFactory(TestMessaging.ContextName))));

        Check(provider).Run();
    }

    private static IStartupCheck Check(ServiceProvider provider) =>
        Assert.Single(provider.GetServices<IStartupCheck>(), check => check is IntegrationEventMapperCheck);

    private static ServiceProvider BuildProvider(
        Action<BuildingBlocksOptions> configure,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocks(options =>
        {
            options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
            configure(options);
        });
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
