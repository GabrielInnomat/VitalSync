using System.Reflection;

namespace GaWeCodes.DependencyInjection.Wiring;

public sealed record IntegrationEventSubscription(
    string EndpointName,
    IReadOnlyList<string> TopicPatterns,
    Assembly ConsumerAssembly);
