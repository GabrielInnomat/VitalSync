using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.DependencyInjection;
using DeadLetterFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;
using Wolverine.Runtime;

using BuildingBlockDefaults = BuildingBlocks.Infrastructure.Messaging.WolverineOptionsExtensions;

namespace BuildingBlocks.Infrastructure.Tests;

[Collection(BrokerAndDatabaseCollection.Name)]
public sealed class IntegrationEventDurabilityTests(PostgreSqlFixture postgres, RabbitMqFixture rabbit)
{
    private const string SubscriberQueueName = "durability-subscriber";

    private const string SinkQueueName = "durability-sink";

    private static readonly TimeSpan DeliveryTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task PublishedIntegrationEvent_ArrivesAtTheBrokerAsAPersistentMessage()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        using var host = await StartHostAsync();
        await using var connection = await OpenConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        await channel.QueueDeclareAsync(
            SinkQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueBindAsync(
            SinkQueueName,
            TestMessaging.ExchangeName,
            "probe.*",
            cancellationToken: TestContext.Current.CancellationToken);

        var name = Guid.NewGuid().ToString();
        await host.Services.GetRequiredService<IMessageBus>()
            .PublishAsync(new DurabilityProbeIntegrationEvent(name));

        var delivered = await WaitForMessageAsync(channel, name);

        Assert.True(
            delivered.BasicProperties.Persistent,
            "The message reached the broker with delivery_mode 1, so a broker restart would drop it.");

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TheSendingEndpointForThePlatformExchange_IsDurable()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        using var host = await StartHostAsync();

        var endpoint = Assert.Single(
            host.Services.GetRequiredService<IWolverineRuntime>().Options.Transports
                .SelectMany(transport => transport.Endpoints()),
            candidate => candidate.Uri.ToString()
                .Contains(TestMessaging.ExchangeName, StringComparison.Ordinal));

        Assert.Equal(EndpointMode.Durable, endpoint.Mode);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TheSubscriberQueue_IsDeclaredAsAQuorumQueue()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        using var host = await StartHostAsync();

        AssertQueueIsQuorum(host, SubscriberQueueName);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TheDeadLetterQueue_IsDeclaredAsAQuorumQueue()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        using var host = await StartHostAsync();

        AssertQueueIsQuorum(host, "wolverine-dead-letter-queue");

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private static void AssertQueueIsQuorum(IHost host, string queueName)
    {
        var queue = Assert.Single(
            host.Services.GetRequiredService<IWolverineRuntime>().Options.Transports
                .OfType<RabbitMqTransport>()
                .Single()
                .Queues,
            candidate => candidate.QueueName == queueName);

        Assert.Equal(QueueType.quorum, queue.QueueType);
        Assert.True(queue.IsDurable, $"'{queueName}' is not durable, so a broker restart would drop it.");
    }

    private static async Task<BasicGetResult> WaitForMessageAsync(IChannel channel, string name)
    {
        var deadline = DateTime.UtcNow + DeliveryTimeout;
        while (true)
        {
            var message = await channel.BasicGetAsync(
                SinkQueueName,
                autoAck: true,
                TestContext.Current.CancellationToken);

            if (message is not null)
            {
                return message;
            }

            Assert.True(DateTime.UtcNow < deadline, $"'{name}' did not reach the broker within {DeliveryTimeout}.");

            await Task.Delay(250, TestContext.Current.CancellationToken);
        }
    }

    private async Task<IConnection> OpenConnectionAsync()
    {
        var factory = new ConnectionFactory { Uri = rabbit.ConnectionUri };
        return await factory.CreateConnectionAsync(TestContext.Current.CancellationToken);
    }

    private async Task<IHost> StartHostAsync() =>
        await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<AttemptRecorder>();

                services.AddBuildingBlocks(options =>
                {
                    options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
                    options.UseMartenEventSourcing(postgres.ConnectionString);
                    options.UseWolverineMessaging(rabbit.ConnectionUri, TestMessaging.ExchangeName, TestMessaging.ContextName);
                    options.SubscribeToIntegrationEvents(
                        SubscriberQueueName,
                        typeof(AlwaysFailsConsumer).Assembly,
                        "upstream.*");
                });
            })
            .UseWolverine(options => options.Durability.Mode = DurabilityMode.Solo)
            .StartAsync(TestContext.Current.CancellationToken);
}

[IntegrationEventTopic("probe.durability")]
public sealed record DurabilityProbeIntegrationEvent(string Name) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
