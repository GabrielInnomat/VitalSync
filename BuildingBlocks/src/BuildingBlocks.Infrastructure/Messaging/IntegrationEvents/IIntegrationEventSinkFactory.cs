using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Infrastructure.Messaging.Transport;

namespace BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;

public interface IIntegrationEventSinkFactory
{
    IIntegrationEventSink Create(IMessageEmitter emitter);
}
