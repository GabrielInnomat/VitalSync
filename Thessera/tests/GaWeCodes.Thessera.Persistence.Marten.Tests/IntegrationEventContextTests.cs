using GaWeCodes.Thessera.Application.IntegrationEvents;
using GaWeCodes.Thessera.Core.DependencyInjection;
using GaWeCodes.Thessera.Core.DependencyInjection.Wiring;
using GaWeCodes.Thessera.Core.Messaging.IntegrationEvents;
using GaWeCodes.Thessera.Messaging.RabbitMq;
using Microsoft.Extensions.DependencyInjection;

namespace GaWeCodes.Thessera.Tests;

public sealed class IntegrationEventContextTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";

    private static readonly Uri RabbitMqUri = new("amqp://localhost:5672");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankContextName_IsRejected(string contextName) =>
        Assert.ThrowsAny<ArgumentException>(() =>
            Configure(options => options.UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, contextName)));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankExchangeName_IsRejected(string exchangeName) =>
        Assert.ThrowsAny<ArgumentException>(() =>
            Configure(options => options.UseWolverineMessaging(RabbitMqUri, exchangeName, TestMessaging.ContextName)));

    [Fact]
    public void ContextNameContainingADot_IsRejectedBecauseItIsProbablyTheExchangeName()
    {
        var thrown = Assert.Throws<ArgumentException>(() =>
            Configure(options => options.UseWolverineMessaging(
                RabbitMqUri,
                TestMessaging.ExchangeName,
                "orders.integration-events")));

        Assert.Contains("wrong position", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Orders")]
    [InlineData("orders_service")]
    [InlineData("-orders")]
    public void ContextNameThatIsNotKebabCase_IsRejected(string contextName) =>
        Assert.Throws<ArgumentException>(() =>
            Configure(options => options.UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, contextName)));

    [Fact]
    public void TheHostGivenNames_AreRecorded()
    {
        var settings = Configure(options => options
            .UseMartenEventStore(ConnectionString)
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName));

        Assert.Equal(
            TestMessaging.ExchangeName,
            ((RabbitMqTransportAdapter)settings.Messaging.Transport!).ExchangeName);
        Assert.Equal(TestMessaging.ContextName, settings.Messaging.Transport.ContextName);
    }

    [Fact]
    public void PublishingUnderTheOwnContext_IsAccepted() =>
        Assert.Equal(
            "probe.own",
            TopicResolver.For(typeof(OwnContextIntegrationEvent), TestMessaging.ContextName));

    [Fact]
    public void PublishingUnderAForeignContext_Throws()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            TopicResolver.For(typeof(ForeignContextIntegrationEvent), TestMessaging.ContextName));

        Assert.Contains("impersonate", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(TestMessaging.ContextName, thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("orders.order-placed", "orders")]
    [InlineData("orders", "orders")]
    public void ContextOf_ReadsTheFirstSegment(string topic, string expected) =>
        Assert.Equal(expected, TopicResolver.ContextOf(topic));

    [Fact]
    public void TheContextName_IsAvailableToTheConsumerSideFilter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddThessera(options => options
            .AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly)
            .UseMartenEventStore(ConnectionString)
            .UseWolverineMessaging(RabbitMqUri, TestMessaging.ExchangeName, TestMessaging.ContextName));

        using var provider = services.BuildServiceProvider();

        Assert.Equal(
            TestMessaging.ContextName,
            provider.GetRequiredService<IntegrationEventSourceContext>().Name);
    }

    private static ThesseraWiringSettings Configure(Action<ThesseraOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddThessera(options =>
        {
            options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
            configure(options);
        });

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ThesseraWiringSettings>();
    }
}

[IntegrationEventTopic("probe.own")]
public sealed record OwnContextIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

[IntegrationEventTopic("upstream.foreign")]
public sealed record ForeignContextIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;
