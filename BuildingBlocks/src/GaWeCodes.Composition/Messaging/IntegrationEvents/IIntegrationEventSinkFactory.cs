using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Messaging.Transport;

namespace GaWeCodes.Messaging.IntegrationEvents;

public interface IIntegrationEventSinkFactory
{
    IIntegrationEventSink Create(IMessageEmitter emitter);
}
