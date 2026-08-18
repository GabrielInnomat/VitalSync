using GaWeCodes.DependencyInjection.Wiring;
using Wolverine;

namespace GaWeCodes.Messaging.Transport;

public interface IWolverineMessagingTransport : IMessagingTransportAdapter
{
    void Configure(WolverineOptions options, bool provisionInfrastructure);

    void ConfigureSubscription(WolverineOptions options, IntegrationEventSubscription subscription);
}
