using System.Reflection;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

public sealed record IntegrationEventSubscription(
    string QueueName,
    IReadOnlyList<string> TopicPatterns,
    Assembly ConsumerAssembly);
