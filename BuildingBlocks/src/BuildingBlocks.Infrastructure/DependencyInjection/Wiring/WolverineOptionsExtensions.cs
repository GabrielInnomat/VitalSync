using System.Text.Json;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Domain.Rules;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using JasperFx;
using Npgsql;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal static class WolverineOptionsExtensions
{
    public const string DomainEventLocalQueueName = "building-blocks-domain-events";

    public const string ProjectionLocalQueueName = "building-blocks-projections";

    public const PartitionSlots DomainEventPartitionSlots = PartitionSlots.Five;

    public static readonly TimeSpan IdempotencyWindow = TimeSpan.FromDays(7);

    public static readonly TimeSpan[] TransientRetryCooldowns =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
    ];

    public static readonly TimeSpan[] UnknownRetryCooldowns =
    [
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(2),
    ];

    public static WolverineOptions ApplyBuildingBlocksIdempotencyWindow(this WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Durability.KeepAfterMessageHandling = IdempotencyWindow;

        return options;
    }

    public static WolverineOptions ApplyBuildingBlocksMessageStorageProvisioning(
        this WolverineOptions options,
        bool provisionInfrastructure)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AutoBuildMessageStorageOnStartup = provisionInfrastructure
            ? AutoCreate.CreateOrUpdate
            : AutoCreate.None;

        return options;
    }

    public static WolverineOptions ApplyBuildingBlocksDomainEventRouting(this WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Discovery.IncludeAssembly(typeof(DomainEventEnvelopeHandler).Assembly);

        options.CodeGeneration.AlwaysUseServiceLocationFor<IIntegrationEventPublisher>();
        options.CodeGeneration.AlwaysUseServiceLocationFor<IIntegrationEventSinkFactory>();
        options.CodeGeneration.AlwaysUseServiceLocationFor<ProjectionRunner>();

        options.CodeGeneration.AlwaysUseServiceLocationFor<ISender>();

        options.MessagePartitioning.ByMessage<DomainEventEnvelope>(PartitionKeyFor);
        options.MessagePartitioning.ByMessage<ProjectionEnvelope>(projection => PartitionKeyFor(projection.Event));

        options.PublishMessage<DomainEventEnvelope>()
            .ToLocalQueue(DomainEventLocalQueueName)
            .PartitionProcessingByGroupId(DomainEventPartitionSlots)
            .UseDurableInbox();

        options.PublishMessage<ProjectionEnvelope>()
            .ToLocalQueue(ProjectionLocalQueueName)
            .PartitionProcessingByGroupId(DomainEventPartitionSlots)
            .UseDurableInbox();

        return options;
    }

    public static string PartitionKeyFor(DomainEventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return $"{envelope.AggregateName}/{envelope.AggregateId}";
    }

    public static WolverineOptions ApplyBuildingBlocksMessagingDefaults(
        this WolverineOptions options,
        MessagingSettings messaging,
        bool provisionInfrastructure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(messaging);

        var transport = options.UseRabbitMq(messaging.RabbitMqUri)
            .UseQuorumQueues()
            .ConfigureChannelCreation(channel =>
            {
                channel.PublisherConfirmationsEnabled = true;
                channel.PublisherConfirmationTrackingEnabled = true;
            });

        if (provisionInfrastructure)
        {
            transport.AutoProvision();
        }

        transport.DeclareExchange(messaging.ExchangeName, exchange => exchange.IsDurable = true);

        options.PublishMessagesToRabbitMqExchange<IIntegrationEvent>(
                messaging.ExchangeName,
                integrationEvent => TopicResolver.For(integrationEvent.GetType(), messaging.ContextName))
            .UseDurableOutbox();

        options.Policies.OnException<JsonException>().MoveToErrorQueue();
        options.Policies.OnException<DomainValidationException>().MoveToErrorQueue();
        options.Policies.OnException<BusinessRuleViolationException>().MoveToErrorQueue();

        options.Policies.OnException<NpgsqlException>(exception => exception.IsTransient)
            .RetryWithCooldown(TransientRetryCooldowns);
        options.Policies.OnException<TimeoutException>()
            .RetryWithCooldown(TransientRetryCooldowns);

        options.Policies.OnException<Exception>()
            .RetryWithCooldown(UnknownRetryCooldowns)
            .Then.MoveToErrorQueue();

        return options;
    }

    public static WolverineOptions ApplyBuildingBlocksSubscription(
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
