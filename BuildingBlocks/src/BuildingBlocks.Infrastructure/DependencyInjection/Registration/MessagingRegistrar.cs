using System.Reflection;
using BuildingBlocks.Domain.Naming;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Registration;

internal sealed class MessagingRegistrar(IServiceCollection services, MessagingSelection messaging)
{
    public void UseMessaging(Uri rabbitMqUri, string exchangeName, string contextName)
    {
        if (!KebabCase.IsValid(contextName))
        {
            throw new ArgumentException(
                $"'{contextName}' is not a valid bounded-context name. It is the first segment of every routing " +
                "key this service publishes, so it must be a single lower-case kebab-case word without a dot " +
                "(for example \"nutrition\"). A value containing a dot is almost always the exchange name passed " +
                "in the wrong position.",
                nameof(contextName));
        }

        services.Replace(ServiceDescriptor.Singleton<IIntegrationEventSinkFactory>(
            new WolverineIntegrationEventSinkFactory(contextName)));
        services.Replace(ServiceDescriptor.Singleton(new IntegrationEventSourceContext(contextName)));
        messaging.SelectTransport(new MessagingSettings(rabbitMqUri, exchangeName, contextName));
    }

    public void Subscribe(string queueName, Assembly consumerAssembly, string[] topicPatterns)
    {
        if (topicPatterns.Length == 0 || Array.Exists(topicPatterns, string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "At least one non-blank topic pattern is required. A queue with no binding receives nothing, " +
                "and neither the broker nor Wolverine reports that as an error.",
                nameof(topicPatterns));
        }

        messaging.SelectSubscription(new IntegrationEventSubscription(
            queueName,
            [.. topicPatterns],
            consumerAssembly));
    }
}
