using BuildingBlocks.Infrastructure.DependencyInjection;
using DeadLetterFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Tests;

[Collection(BrokerAndDatabaseCollection.Name)]
public sealed class BrokerTopologyCheckTests(PostgreSqlFixture postgres, RabbitMqFixture rabbit)
{
    [Fact]
    public async Task AMissingExchange_FailsTheStartInsteadOfSwallowingEveryPublish()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        var exchangeName = TestMessaging.UniqueExchangeName("absent-exchange");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            StartAsync(exchangeName, queueName: null, InfrastructureProvisioning.Never));

        Assert.Contains(exchangeName, thrown.Message, StringComparison.Ordinal);
        Assert.Contains("ADR-0037", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMissingQueue_FailsTheStartWithAMessageNamingIt()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        var exchangeName = TestMessaging.UniqueExchangeName("queue-probe");
        var queueName = TestMessaging.UniqueQueueName("queue-probe");

        using (var provisioner = await StartAsync(exchangeName, queueName, InfrastructureProvisioning.AtStartup))
        {
            await provisioner.StopAsync(TestContext.Current.CancellationToken);
        }

        var absentQueue = TestMessaging.UniqueQueueName("absent-queue");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            StartAsync(exchangeName, absentQueue, InfrastructureProvisioning.Never));

        Assert.Contains(absentQueue, thrown.Message, StringComparison.Ordinal);
        Assert.Contains("ADR-0037", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AProvisionedTopology_LetsAConsumingHostStart()
    {
        Assert.SkipUnless(postgres.Available, postgres.SkipReason);
        Assert.SkipUnless(rabbit.Available, rabbit.SkipReason);

        var exchangeName = TestMessaging.UniqueExchangeName("provisioned");
        var queueName = TestMessaging.UniqueQueueName("provisioned");

        using (var provisioner = await StartAsync(exchangeName, queueName, InfrastructureProvisioning.AtStartup))
        {
            await provisioner.StopAsync(TestContext.Current.CancellationToken);
        }

        using var consumer = await StartAsync(exchangeName, queueName, InfrastructureProvisioning.Never);
        await consumer.StopAsync(TestContext.Current.CancellationToken);
    }

    private async Task<IHost> StartAsync(
        string exchangeName,
        string? queueName,
        InfrastructureProvisioning provisioning) =>
        await Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddBuildingBlocks(options =>
            {
                options.AddDomainEventsFrom(typeof(FlushProbeStarted).Assembly);
                options.UseMartenEventSourcing(postgres.ConnectionString)
                    .ProvisionInfrastructure(provisioning);
                options.UseWolverineMessaging(
                    rabbit.ConnectionUri,
                    exchangeName,
                    TestMessaging.ContextName);

                if (queueName is not null)
                {
                    options.SubscribeToIntegrationEvents(
                        queueName,
                        typeof(AlwaysFailsConsumer).Assembly,
                        "upstream.*");
                }
            }))
            .UseWolverine(options => options.Durability.Mode = DurabilityMode.Solo)
            .StartAsync(TestContext.Current.CancellationToken);
}
