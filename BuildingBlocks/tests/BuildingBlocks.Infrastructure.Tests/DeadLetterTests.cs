using System.Text;
using BuildingBlocks.Infrastructure.DependencyInjection;
using DeadLetterFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Tests;

// ApplyBuildingBlockMessagingDefaults promises three retries with a cooldown and then the error queue. Until
// this test that promise had never been observed: no message in the walking skeleton ever failed, so a policy
// that silently did nothing would have looked exactly the same. Both halves matter - retrying forever blocks
// the queue, giving up immediately loses the message.
[Collection(BrokerAndDatabaseCollection.Name)]
public sealed class DeadLetterTests(PostgreSqlFixture postgres, RabbitMqFixture rabbit)
{
    private const string QueueName = "dead-letter-probe";

    // Wolverine's own dead-letter queue, declared on the broker rather than in the message store. This is the
    // surprise the test uncovered: with the RabbitMQ transport a poison message does not end up in
    // wolverine_dead_letters in PostgreSQL, where an operator looking at the write database would search for
    // it, but on the broker.
    private const string DeadLetterQueueName = "wolverine-dead-letter-queue";

    // One initial delivery plus the three retries the policy declares.
    private const int ExpectedAttempts = 4;

    // Three cooldowns of 100ms, 500ms and 2s, plus broker round trips.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task AConsumerThatAlwaysFails_IsRetriedAndThenDeadLettered()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        var recorder = new AttemptRecorder();
        using var host = await StartHostAsync(recorder);
        var name = Guid.NewGuid().ToString();

        await host.Services.GetRequiredService<IMessageBus>()
            .PublishAsync(new AlwaysFailsIntegrationEvent(name));

        // The message survives its own failure: it ends up somewhere an operator can find it, not dropped.
        // Waiting for it rather than for a fixed delay is what makes this an observation instead of a guess.
        var deadLettered = await WaitForDeadLetterAsync(name, recorder);
        Assert.Contains(name, deadLettered, StringComparison.Ordinal);

        // Exactly four: fewer means the retries never happened, more means the policy never gave up and the
        // consumer is still burning through a message it can never handle.
        Assert.Equal(ExpectedAttempts, recorder.Attempts);

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

            // The attempt count separates the two ways this can fail: zero attempts means the message never
            // arrived at all, four means it arrived and was retried but the dead letter went elsewhere.
            Assert.True(
                DateTime.UtcNow < deadline,
                $"The message was not dead-lettered within {Timeout}. Handler attempts: {recorder.Attempts}.");

            await Task.Delay(250, TestContext.Current.CancellationToken);
        }
    }

    private async Task<IHost> StartHostAsync(AttemptRecorder recorder) =>
        await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(recorder);

                // The production wiring, not a hand-rolled copy: deleting the retry policy from
                // ApplyBuildingBlockMessagingDefaults fails this test.
                services.AddBuildingBlocks(options =>
                {
                    // Marten supplies Wolverine's message store, which the durable inbox of a subscription
                    // requires. The host would not start without it.
                    options.UseMartenEventSourcing(postgres.ConnectionString);
                    options.UseWolverineMessaging(rabbit.ConnectionUri);
                    options.SubscribeToIntegrationEvents(
                        QueueName,
                        typeof(AlwaysFailsConsumer).Assembly,
                        "probe.*");
                });
            })
            .UseWolverine(options => options.Durability.Mode = DurabilityMode.Solo)
            .StartAsync(TestContext.Current.CancellationToken);
}

[CollectionDefinition(Name)]
public sealed class BrokerAndDatabaseCollection
    : ICollectionFixture<PostgreSqlFixture>, ICollectionFixture<RabbitMqFixture>
{
    public const string Name = "BrokerAndDatabase";
}
