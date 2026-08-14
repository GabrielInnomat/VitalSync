using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.DependencyInjection.Validation;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using BuildingBlocks.Infrastructure.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ValidHandlersFixture;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class IntegrationEventMapperCheckTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test";

    private static readonly Uri RabbitMqUri = new("amqp://localhost:5672");

    [Fact]
    public async Task MapperWithoutATransport_FailsNamingTheMapper()
    {
        using var provider = BuildProvider(options => options.AddHandlersFrom(typeof(RegistrationMapper).Assembly));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Check(provider).RunAsync(TestContext.Current.CancellationToken));

        Assert.Contains(nameof(RegistrationMapper), exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            nameof(RabbitMqMessagingExtensions.UseWolverineMessaging),
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task NoMapper_Passes()
    {
        using var provider = BuildProvider(_ => { });

        await Check(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MapperWithATransport_Passes()
    {
        using var provider = BuildProvider(options => options
            .AddHandlersFrom(typeof(RegistrationMapper).Assembly)
            .UseMartenEventSourcing(ConnectionString)
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName));

        await Check(provider).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MapperAndAHostSuppliedSinkFactory_Passes()
    {
        using var provider = BuildProvider(
            options => options.AddHandlersFrom(typeof(RegistrationMapper).Assembly),
            services => services.Replace(ServiceDescriptor.Singleton<IIntegrationEventSinkFactory>(
                new IntegrationEventSinkFactory(TestMessaging.ContextName))));

        await Check(provider).RunAsync(TestContext.Current.CancellationToken);
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
            options.AddDomainEventsFrom(typeof(RegistrationEvent).Assembly);
            configure(options);
        });
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
