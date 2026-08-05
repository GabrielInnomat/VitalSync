using BuildingBlocks.Application.IntegrationEvents;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;

public interface IIntegrationEventSinkFactory
{
    IIntegrationEventSink Create(IMessageContext context);
}
