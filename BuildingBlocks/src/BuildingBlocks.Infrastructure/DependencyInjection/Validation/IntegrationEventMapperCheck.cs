using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal sealed class IntegrationEventMapperCheck(IServiceProvider serviceProvider) : SynchronousStartupCheck
{
    public override StartupPhase Phase => StartupPhase.BeforeHostedServicesStart;

    protected override void Run()
    {
        if (serviceProvider.GetService<IIntegrationEventSinkFactory>() is not NullIntegrationEventSinkFactory)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var mappers = scope.ServiceProvider
            .GetServices<IIntegrationEventMapper>()
            .Select(mapper => $"'{mapper.GetType()}'")
            .ToList();

        if (mappers.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Integration-event mappers are registered, but no messaging transport is configured: " +
            $"{string.Join(", ", mappers)}. A mapper exists for one purpose — turning a domain event into an " +
            "integration event that leaves this context — so every event it produces would be handed to the null " +
            "sink and dropped after a log warning, while the commit reports success and every downstream context " +
            "silently stops receiving. Call options.UseWolverineMessaging(rabbitMqUri, exchangeName, contextName), " +
            "or delete the mapper if this context publishes nothing.");
    }
}
