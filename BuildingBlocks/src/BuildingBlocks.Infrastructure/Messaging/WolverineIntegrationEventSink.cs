using BuildingBlocks.Application;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

internal sealed class WolverineIntegrationEventSink(IMessageContext context) : IIntegrationEventSink
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return context.PublishAsync(integrationEvent).AsTask();
    }
}

internal sealed class WolverineIntegrationEventSinkFactory : IIntegrationEventSinkFactory
{
    public IIntegrationEventSink Create(IMessageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new WolverineIntegrationEventSink(context);
    }
}
