using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using RabbitMQ.Client;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal sealed class BrokerTopologyCheck(WolverineWiringSettings settings) : IStartupCheck
{
    public StartupPhase Phase => StartupPhase.BeforeHostedServicesStart;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (settings.ProvisionsInfrastructure || settings.Messaging is not { } messaging)
        {
            return;
        }

        var factory = new ConnectionFactory { Uri = messaging.RabbitMqUri };
        var connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var closingConnection = connection.ConfigureAwait(false);

        await AssertExistsAsync(
            connection,
            channel => channel.ExchangeDeclarePassiveAsync(messaging.ExchangeName, cancellationToken),
            $"the exchange '{messaging.ExchangeName}' does not exist on the broker. Wolverine would still start and " +
            "every publish would return successfully while the broker discards the message, so no consumer would " +
            "ever see it",
            cancellationToken).ConfigureAwait(false);

        if (settings.Subscription is not { } subscription)
        {
            return;
        }

        await AssertExistsAsync(
            connection,
            channel => channel.QueueDeclarePassiveAsync(subscription.QueueName, cancellationToken),
            $"the queue '{subscription.QueueName}' does not exist on the broker. Wolverine's listener would fail " +
            "with a bare AMQP 404 that names neither this host nor the reason",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task AssertExistsAsync(
        IConnection connection,
        Func<IChannel, Task> declarePassive,
        string complaint,
        CancellationToken cancellationToken)
    {
        Exception? failure = null;

        try
        {
            var channel = await connection
                .CreateChannelAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await using var closingChannel = channel.ConfigureAwait(false);

            await declarePassive(channel).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            failure = exception;
        }

        if (failure is null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"This host does not provision infrastructure, and {complaint}. Run the context's migration worker — " +
            "the one host that selects ProvisionInfrastructure(InfrastructureProvisioning.AtStartup) — before " +
            "starting this one.",
            failure);
    }
}
