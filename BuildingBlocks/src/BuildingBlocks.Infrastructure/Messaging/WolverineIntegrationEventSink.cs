using BuildingBlocks.Application;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

internal sealed class WolverineIntegrationEventSink(IMessageContext context, string sourceContext) : IIntegrationEventSink
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var delivery = new DeliveryOptions();
        delivery.Headers[IntegrationEventSourceContext.HeaderName] = sourceContext;

        return context.PublishAsync(integrationEvent, delivery).AsTask();
    }
}

internal sealed class WolverineIntegrationEventSinkFactory(string sourceContext) : IIntegrationEventSinkFactory
{
    public IIntegrationEventSink Create(IMessageContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new WolverineIntegrationEventSink(context, sourceContext);
    }
}
