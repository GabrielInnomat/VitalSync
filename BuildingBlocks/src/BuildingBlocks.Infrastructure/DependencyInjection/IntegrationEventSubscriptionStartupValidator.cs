using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine.Runtime;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

internal sealed class IntegrationEventSubscriptionStartupValidator(
    IServiceProvider serviceProvider,
    WolverineWiringSettings settings) : IHostedLifecycleService
{
    public Task StartedAsync(CancellationToken cancellationToken)
    {
        Validate();
        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void Validate()
    {
        if (settings.Messaging is not { } messaging || settings.Subscription is not { } subscription)
        {
            return;
        }

        if (serviceProvider.GetService<IWolverineRuntime>() is not WolverineRuntime runtime)
        {
            return;
        }

        var handledIntegrationEvents = runtime.Handlers.Chains
            .Where(chain => chain.Handlers.Any(call => call.HandlerType.Assembly == subscription.ConsumerAssembly))
            .Select(chain => chain.MessageType)
            .Where(messageType => messageType.IsAssignableTo(typeof(IIntegrationEvent)))
            .Where(messageType => !messageType.IsAbstract && !messageType.IsInterface)
            .Distinct();

        var problems = new List<string>();

        foreach (var messageType in handledIntegrationEvents)
        {
            var topic = IntegrationEventTopic.For(messageType);

            if (IntegrationEventTopic.ContextOf(topic).Equals(messaging.ContextName, StringComparison.Ordinal))
            {
                problems.Add(
                    $"'{messageType.FullName}' publishes under '{topic}', which belongs to this very context " +
                    $"('{messaging.ContextName}'). A context does not consume its own integration events — such a " +
                    "message is discarded on arrival, so the handler would never run. Call the domain directly " +
                    "instead of going through the broker.");
                continue;
            }

            if (!subscription.TopicPatterns.Any(pattern => TopicPatternMatcher.Matches(pattern, topic)))
            {
                problems.Add(
                    $"'{messageType.FullName}' is handled here but its topic '{topic}' matches none of the bound " +
                    $"patterns [{string.Join(", ", subscription.TopicPatterns)}]. The queue would never receive it, " +
                    "and an empty queue looks exactly like an upstream context that has not published yet. Bind a " +
                    "matching pattern in SubscribeToIntegrationEvents.");
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "The integration-event subscription does not cover every handler this service registers:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, problems.Select(problem => "- " + problem)));
        }
    }
}
