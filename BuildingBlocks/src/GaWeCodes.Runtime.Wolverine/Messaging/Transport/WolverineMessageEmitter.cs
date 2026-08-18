using Wolverine;

namespace GaWeCodes.Messaging.Transport;

internal sealed class WolverineMessageEmitter(IMessageContext context) : IMessageEmitter
{
    public Task PublishAsync(object message, IReadOnlyDictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (headers is null || headers.Count == 0)
        {
            return context.PublishAsync(message).AsTask();
        }

        var delivery = new DeliveryOptions();

        foreach (var header in headers)
        {
            delivery.Headers[header.Key] = header.Value;
        }

        return context.PublishAsync(message, delivery).AsTask();
    }
}
