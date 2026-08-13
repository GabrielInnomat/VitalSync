using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Infrastructure.Messaging.Transport;

namespace BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;

internal sealed class IntegrationEventSink(IMessageEmitter emitter, string sourceContext) : IIntegrationEventSink
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return emitter.PublishAsync(
            integrationEvent,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [IntegrationEventSourceContext.HeaderName] = sourceContext,
            },
            cancellationToken);
    }
}

internal sealed class IntegrationEventSinkFactory(string sourceContext) : IIntegrationEventSinkFactory
{
    public IIntegrationEventSink Create(IMessageEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(emitter);
        return new IntegrationEventSink(emitter, sourceContext);
    }
}
