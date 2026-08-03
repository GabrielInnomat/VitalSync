using System.Collections.Concurrent;
using System.Reflection;
using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Messaging;

internal static class IntegrationEventTopic
{
    private static readonly ConcurrentDictionary<Type, string> Topics = new();

    public static string For(Type integrationEventType)
    {
        ArgumentNullException.ThrowIfNull(integrationEventType);

        return Topics.GetOrAdd(integrationEventType, static type =>
            type.GetCustomAttribute<IntegrationEventTopicAttribute>(inherit: false)?.Topic
            ?? throw new InvalidOperationException(
                $"The integration event '{type.FullName}' carries no [IntegrationEventTopic] attribute. " +
                "The topic is the routing key on the platform exchange and part of the published contract; " +
                "without it the event would be published under a key no consumer has bound and silently " +
                "disappear. Declare it as [IntegrationEventTopic(\"<context>.<event>\")]."));
    }
}
