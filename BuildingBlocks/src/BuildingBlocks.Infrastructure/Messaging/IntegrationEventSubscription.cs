using System.Reflection;

namespace BuildingBlocks.Infrastructure.Messaging;

internal sealed record IntegrationEventSubscription(
    string QueueName,
    IReadOnlyList<string> TopicPatterns,
    Assembly ConsumerAssembly);
