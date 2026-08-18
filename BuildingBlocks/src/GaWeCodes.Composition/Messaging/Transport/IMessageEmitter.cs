namespace GaWeCodes.Messaging.Transport;

public interface IMessageEmitter
{
    Task PublishAsync(object message, IReadOnlyDictionary<string, string>? headers, CancellationToken cancellationToken);
}
