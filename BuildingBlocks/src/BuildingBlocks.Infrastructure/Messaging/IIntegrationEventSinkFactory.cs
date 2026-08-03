using BuildingBlocks.Application;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

public interface IIntegrationEventSinkFactory
{
    IIntegrationEventSink Create(IMessageContext context);
}
