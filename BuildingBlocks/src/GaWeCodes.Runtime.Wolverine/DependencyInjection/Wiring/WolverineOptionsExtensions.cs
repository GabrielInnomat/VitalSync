using System.Text.Json;
using GaWeCodes.Application.Cqrs;
using GaWeCodes.Application.DomainEvents;
using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Domain.Rules;
using GaWeCodes.Messaging.DomainEvents;
using GaWeCodes.Messaging.IntegrationEvents;
using JasperFx;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.ErrorHandling;

namespace GaWeCodes.DependencyInjection.Wiring;

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

    public static WolverineOptions ApplyBuildingBlocksMessagingPolicies(
        this WolverineOptions options,
        Func<Exception, bool> isTransientFault)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(isTransientFault);

        options.Policies.OnException<JsonException>().MoveToErrorQueue();
        options.Policies.OnException<DomainValidationException>().MoveToErrorQueue();
        options.Policies.OnException<BusinessRuleViolationException>().MoveToErrorQueue();

        options.Policies.OnException<Exception>(isTransientFault)
            .RetryWithCooldown(TransientRetryCooldowns);
        options.Policies.OnException<TimeoutException>()
            .RetryWithCooldown(TransientRetryCooldowns);

        options.Policies.OnException<Exception>()
            .RetryWithCooldown(UnknownRetryCooldowns)
            .Then.MoveToErrorQueue();

        return options;
    }

    public static WolverineOptions ApplyBuildingBlocksIntegrationEventTopics(
        this WolverineOptions options,
        string contextName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextName);

        options.Policies.AllSenders(sender => sender.CustomizeOutgoing(envelope =>
        {
            if (envelope.Message is IIntegrationEvent)
            {
                envelope.TopicName = TopicResolver.For(envelope.Message.GetType(), contextName);
            }
        }));

        return options;
    }

    public static WolverineOptions ApplyBuildingBlocksSubscriptionDiscovery(
        this WolverineOptions options,
        IntegrationEventSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(subscription);

        options.Discovery.IncludeAssembly(subscription.ConsumerAssembly);

        options.CodeGeneration.AlwaysUseServiceLocationFor<IntegrationEventSourceContext>();
        options.Policies.AddMiddleware(
            typeof(OwnContextIntegrationEventFilter),
            chain => chain.MessageType.IsAssignableTo(typeof(IIntegrationEvent)));

        return options;
    }
}
