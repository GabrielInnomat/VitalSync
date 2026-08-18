using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Messaging.Transport;
using Wolverine;
using Wolverine.Kafka;

namespace ForeignBrokerHost;

public sealed class KafkaTransportAdapter(string bootstrapServers, string contextName) : IWolverineMessagingTransport
{
    public string Description => "UseKafkaMessaging";

    public string ContextName => contextName;

    public void Register(MessagingTransportRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.UseWolverineRuntime();
    }

    public void Configure(WolverineOptions options, bool provisionInfrastructure)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.UseKafka(bootstrapServers);

        options.Publish(rule => rule.MessagesImplementing<IIntegrationEvent>()
            .ToKafkaTopics()
            .UseDurableOutbox());
    }

    public void ConfigureSubscription(WolverineOptions options, IntegrationEventSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(subscription);

        options.ListenToKafkaTopic(subscription.EndpointName).UseDurableInbox();
    }
}
