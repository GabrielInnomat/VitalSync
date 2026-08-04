using System.Reflection;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal sealed record IntegrationEventSubscription(
    string QueueName,
    IReadOnlyList<string> TopicPatterns,
    Assembly ConsumerAssembly);
