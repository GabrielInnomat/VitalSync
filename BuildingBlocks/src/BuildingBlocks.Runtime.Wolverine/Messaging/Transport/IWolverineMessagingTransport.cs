using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging.Transport;

public interface IWolverineMessagingTransport : IMessagingTransportAdapter
{
    void Configure(WolverineOptions options, bool provisionInfrastructure);

    void ConfigureSubscription(WolverineOptions options, IntegrationEventSubscription subscription);
}
