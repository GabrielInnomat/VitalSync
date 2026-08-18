using System.Reflection;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

public sealed record IntegrationEventSubscription(
    string EndpointName,
    IReadOnlyList<string> TopicPatterns,
    Assembly ConsumerAssembly);
