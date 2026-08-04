using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using BuildingBlocks.Infrastructure.Messaging;
using DeadLetterFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Wolverine.RabbitMQ;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Tests;

[Collection(BrokerAndDatabaseCollection.Name)]
public sealed class IntegrationEventSubscriptionValidationTests(PostgreSqlFixture postgres, RabbitMqFixture rabbit)
{
    private const string UpstreamTopic = "upstream.always-fails";

    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task AHandlerWhoseTopicNoBoundPatternMatches_FailsTheStart()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            StartConsumerAsync("validation-no-match", TestMessaging.ContextName, "somewhere-else.*"));

        Assert.Contains(nameof(AlwaysFailsIntegrationEvent), thrown.Message, StringComparison.Ordinal);
        Assert.Contains(UpstreamTopic, thrown.Message, StringComparison.Ordinal);
        Assert.Contains("somewhere-else.*", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AHandlerForAnEventOfTheOwnContext_FailsTheStart()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            StartConsumerAsync("validation-own-context", TestMessaging.UpstreamContextName, "upstream.*"));

        Assert.Contains("this very context", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(TestMessaging.UpstreamContextName, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMatchingPatternFromAForeignContext_Starts()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        using var host = await StartConsumerAsync(
            "validation-happy",
            TestMessaging.ContextName,
            "upstream.*");

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AnEventCarryingTheOwnContextAsSource_IsDiscardedBeforeAnyHandlerRuns()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        const string queueName = "self-consumption-probe";

        var recorder = new AttemptRecorder();
        using var host = await StartConsumerAsync(queueName, TestMessaging.ContextName, "upstream.*", recorder);
        using var publisher = await StartPublisherAsync();

        await publisher.Services.GetRequiredService<IMessageBus>().PublishAsync(
            new AlwaysFailsIntegrationEvent("suppressed"),
            SourceContextHeader(TestMessaging.ContextName));

        await WaitForTheQueueToDrainAsync(queueName);

        Assert.Equal(0, recorder.Attempts);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AnEventCarryingAForeignContextAsSource_ReachesTheHandler()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        const string queueName = "foreign-source-probe";

        var recorder = new AttemptRecorder();
        using var host = await StartConsumerAsync(queueName, TestMessaging.ContextName, "upstream.*", recorder);
        using var publisher = await StartPublisherAsync();

        await publisher.Services.GetRequiredService<IMessageBus>().PublishAsync(
            new AlwaysFailsIntegrationEvent("delivered"),
            SourceContextHeader(TestMessaging.UpstreamContextName));

        var deadline = DateTime.UtcNow + Grace;
        while (recorder.Attempts == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        Assert.True(recorder.Attempts > 0, "The consumer never saw a message published by a foreign context.");

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private static DeliveryOptions SourceContextHeader(string contextName)
    {
        var delivery = new DeliveryOptions();
        delivery.Headers[IntegrationEventSourceContext.HeaderName] = contextName;
        return delivery;
    }

    private async Task WaitForTheQueueToDrainAsync(string queueName)
    {
        var factory = new ConnectionFactory { Uri = rabbit.ConnectionUri };
        await using var connection = await factory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow + Grace;
        while (DateTime.UtcNow < deadline)
        {
            var declared = await channel.QueueDeclarePassiveAsync(
                queueName,
                TestContext.Current.CancellationToken);

            if (declared.MessageCount == 0)
            {
                await Task.Delay(500, TestContext.Current.CancellationToken);
                return;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private async Task<IHost> StartConsumerAsync(
        string queueName,
        string contextName,
        string topicPattern,
        AttemptRecorder? recorder = null) =>
        await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(recorder ?? new AttemptRecorder());

                services.AddBuildingBlocks(options =>
                {
                    options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
                    options.UseMartenEventSourcing(postgres.ConnectionString);
                    options.UseWolverineMessaging(rabbit.ConnectionUri, TestMessaging.ExchangeName, contextName);
                    options.SubscribeToIntegrationEvents(
                        queueName,
                        typeof(AlwaysFailsConsumer).Assembly,
                        topicPattern);
                });
            })
            .UseWolverine(options => options.Durability.Mode = DurabilityMode.Solo)
            .StartAsync(TestContext.Current.CancellationToken);

    private async Task<IHost> StartPublisherAsync() =>
        await Host.CreateDefaultBuilder()
            .UseWolverine(options =>
            {
                options.Durability.Mode = DurabilityMode.Solo;

                options.UseRabbitMq(rabbit.ConnectionUri)
                    .AutoProvision()
                    .DeclareExchange(TestMessaging.ExchangeName, exchange => exchange.IsDurable = true);

                options.PublishMessagesToRabbitMqExchange<AlwaysFailsIntegrationEvent>(
                    TestMessaging.ExchangeName,
                    _ => UpstreamTopic);
            })
            .StartAsync(TestContext.Current.CancellationToken);
}
