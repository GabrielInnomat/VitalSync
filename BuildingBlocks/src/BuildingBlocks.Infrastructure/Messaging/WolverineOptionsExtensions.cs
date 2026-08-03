using BuildingBlocks.Application;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace BuildingBlocks.Infrastructure.Messaging;

internal static class WolverineOptionsExtensions
{
    public const string DomainEventLocalQueueName = "building-blocks-domain-events";

    public const string IntegrationEventExchangeName = "vitalsync.integration-events";

    public static WolverineOptions ApplyBuildingBlockDomainEventRouting(this WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Discovery.IncludeAssembly(typeof(DomainEventEnvelopeHandler).Assembly);

        options.CodeGeneration.AlwaysUseServiceLocationFor<IDomainEventPublisher>();
        options.CodeGeneration.AlwaysUseServiceLocationFor<IIntegrationEventSinkFactory>();

        options.CodeGeneration.AlwaysUseServiceLocationFor<ISender>();

        options.PublishMessage<DomainEventEnvelope>()
            .ToLocalQueue(DomainEventLocalQueueName)
            .Sequential()
            .UseDurableInbox();

        return options;
    }

    public static WolverineOptions ApplyBuildingBlockMessagingDefaults(this WolverineOptions options, Uri rabbitMqUri)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(rabbitMqUri);

        options.UseRabbitMq(rabbitMqUri).AutoProvision();

        options.PublishMessagesToRabbitMqExchange<IIntegrationEvent>(
            IntegrationEventExchangeName,
            integrationEvent => IntegrationEventTopic.For(integrationEvent.GetType()));

        options.Policies.OnException<Exception>()
            .RetryWithCooldown(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromSeconds(2))
            .Then.MoveToErrorQueue();

        return options;
    }

    public static WolverineOptions ApplyBuildingBlockSubscription(
        this WolverineOptions options,
        IntegrationEventSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(subscription);

        options.Discovery.IncludeAssembly(subscription.ConsumerAssembly);

        options.ListenToRabbitQueue(subscription.QueueName).UseDurableInbox();

        var exchange = options.UseRabbitMq().BindExchange(IntegrationEventExchangeName);
        foreach (var topicPattern in subscription.TopicPatterns)
        {
            exchange.ToQueue(subscription.QueueName, bindingKey: topicPattern);
        }

        return options;
    }
}
