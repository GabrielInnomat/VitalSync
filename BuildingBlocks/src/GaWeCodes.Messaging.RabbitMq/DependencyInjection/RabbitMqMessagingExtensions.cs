namespace GaWeCodes.DependencyInjection;

public static class RabbitMqMessagingExtensions
{
    public static BuildingBlocksOptions UseWolverineMessaging(
        this BuildingBlocksOptions options,
        Uri rabbitMqUri,
        string exchangeName,
        string contextName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(rabbitMqUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        return options.UseMessagingTransport(new RabbitMqTransportAdapter(rabbitMqUri, exchangeName, contextName));
    }
}
