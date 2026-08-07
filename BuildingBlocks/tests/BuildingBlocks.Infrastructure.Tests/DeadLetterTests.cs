using System.Text;
using BuildingBlocks.Infrastructure.DependencyInjection;
using DeadLetterFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Wolverine;
using Wolverine.RabbitMQ;

namespace BuildingBlocks.Infrastructure.Tests;

[Collection(BrokerAndDatabaseCollection.Name)]
public sealed class DeadLetterTests(PostgreSqlFixture postgres, RabbitMqFixture rabbit)
{
    private const string DeadLetterQueueName = "wolverine-dead-letter-queue";

    private const int ExpectedAttempts = 4;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task AConsumerThatAlwaysFails_IsRetriedAndThenDeadLettered()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        var recorder = new AttemptRecorder();
        using var host = await StartHostAsync(recorder, TestMessaging.UniqueQueueName("dead-letter-probe"));
        using var upstream = await StartUpstreamPublisherAsync();
        var name = Guid.NewGuid().ToString();

        await upstream.Services.GetRequiredService<IMessageBus>()
            .PublishAsync(new AlwaysFailsIntegrationEvent(name));

        var deadLettered = await WaitForDeadLetterAsync(name, recorder);
        Assert.Contains(name, deadLettered, StringComparison.Ordinal);

        Assert.Equal(ExpectedAttempts, recorder.Attempts);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ADeterministicFailure_IsDeadLetteredWithoutBeingRetried()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        var recorder = new AttemptRecorder();
        using var host = await StartHostAsync(recorder, TestMessaging.UniqueQueueName("dead-letter-invalid-probe"));
        using var upstream = await StartUpstreamPublisherAsync();
        var name = Guid.NewGuid().ToString();

        await upstream.Services.GetRequiredService<IMessageBus>()
            .PublishAsync(new AlwaysInvalidIntegrationEvent(name));

        var deadLettered = await WaitForDeadLetterAsync(name, recorder);
        Assert.Contains(name, deadLettered, StringComparison.Ordinal);

        Assert.Equal(1, recorder.Names.Count(recorded => recorded == name));

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private async Task<string> WaitForDeadLetterAsync(string name, AttemptRecorder recorder)
    {
        var factory = new ConnectionFactory { Uri = rabbit.ConnectionUri };
        await using var connection = await factory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow + Timeout;
        while (true)
        {
            var message = await channel.BasicGetAsync(
                DeadLetterQueueName,
                autoAck: true,
                TestContext.Current.CancellationToken);

            if (message is not null)
            {
                var body = Encoding.UTF8.GetString(message.Body.Span);
                if (body.Contains(name, StringComparison.Ordinal))
                {
                    return body;
                }
            }

            Assert.True(
                DateTime.UtcNow < deadline,
                $"The message was not dead-lettered within {Timeout}. Handler attempts: {recorder.Attempts}.");

            await Task.Delay(250, TestContext.Current.CancellationToken);
        }
    }

    private async Task<IHost> StartHostAsync(AttemptRecorder recorder, string queueName) =>
        await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(recorder);

                services.AddBuildingBlocks(options =>
                {
                    options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
                    options.UseMartenEventSourcing(postgres.ConnectionString);
                    options.UseWolverineMessaging(rabbit.ConnectionUri, TestMessaging.ExchangeName, TestMessaging.ContextName);
                    options.SubscribeToIntegrationEvents(
                        queueName,
                        typeof(AlwaysFailsConsumer).Assembly,
                        "upstream.*");
                });
            })
            .UseWolverine(options => options.Durability.Mode = DurabilityMode.Solo)
            .StartAsync(TestContext.Current.CancellationToken);

    private async Task<IHost> StartUpstreamPublisherAsync() =>
        await Host.CreateDefaultBuilder()
            .UseWolverine(options =>
            {
                options.Durability.Mode = DurabilityMode.Solo;

                options.UseRabbitMq(rabbit.ConnectionUri)
                    .AutoProvision()
                    .DeclareExchange(TestMessaging.ExchangeName, exchange => exchange.IsDurable = true);

                options.PublishMessagesToRabbitMqExchange<AlwaysFailsIntegrationEvent>(
                    TestMessaging.ExchangeName,
                    _ => "upstream.always-fails");

                options.PublishMessagesToRabbitMqExchange<AlwaysInvalidIntegrationEvent>(
                    TestMessaging.ExchangeName,
                    _ => "upstream.always-invalid");
            })
            .StartAsync(TestContext.Current.CancellationToken);
}

[CollectionDefinition(Name)]
public sealed class BrokerAndDatabaseCollection
    : ICollectionFixture<PostgreSqlFixture>, ICollectionFixture<RabbitMqFixture>
{
    public const string Name = "BrokerAndDatabase";
}
