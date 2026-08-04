using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;
using Wolverine;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal static class WolverineOptionsExtensions
{
    public const string DomainEventLocalQueueName = "building-blocks-domain-events";

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

    public static WolverineOptions ApplyBuildingBlockMessagingDefaults(
        this WolverineOptions options,
        MessagingSettings messaging)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(messaging);

        options.UseRabbitMq(messaging.RabbitMqUri)
            .AutoProvision()
            .UseQuorumQueues()
            .DeclareExchange(messaging.ExchangeName, exchange => exchange.IsDurable = true);

        options.PublishMessagesToRabbitMqExchange<IIntegrationEvent>(
                messaging.ExchangeName,
                integrationEvent => IntegrationEventTopic.For(integrationEvent.GetType(), messaging.ContextName))
            .UseDurableOutbox();

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
        IntegrationEventSubscription subscription,
        string exchangeName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName);

        options.Discovery.IncludeAssembly(subscription.ConsumerAssembly);

        options.CodeGeneration.AlwaysUseServiceLocationFor<IntegrationEventSourceContext>();
        options.Policies.AddMiddleware(
            typeof(OwnContextIntegrationEventFilter),
            chain => chain.MessageType.IsAssignableTo(typeof(IIntegrationEvent)));

        options.ListenToRabbitQueue(subscription.QueueName, queue => queue.IsDurable = true).UseDurableInbox();

        var exchange = options.UseRabbitMq().BindExchange(exchangeName);
        foreach (var topicPattern in subscription.TopicPatterns)
        {
            exchange.ToQueue(subscription.QueueName, bindingKey: topicPattern);
        }

        return options;
    }
}
