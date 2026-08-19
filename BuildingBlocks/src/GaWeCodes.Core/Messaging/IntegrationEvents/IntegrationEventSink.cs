using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Core.Messaging.Transport;

namespace GaWeCodes.Core.Messaging.IntegrationEvents;

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
