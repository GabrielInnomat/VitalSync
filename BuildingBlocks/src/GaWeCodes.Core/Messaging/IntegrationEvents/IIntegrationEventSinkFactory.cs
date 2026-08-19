using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Core.Messaging.Transport;

namespace GaWeCodes.Core.Messaging.IntegrationEvents;

public interface IIntegrationEventSinkFactory
{
    IIntegrationEventSink Create(IMessageEmitter emitter);
}
