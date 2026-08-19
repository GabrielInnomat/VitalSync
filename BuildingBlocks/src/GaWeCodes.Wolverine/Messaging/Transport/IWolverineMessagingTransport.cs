using GaWeCodes.Core.DependencyInjection.Wiring;
using GaWeCodes.Core.Messaging.Transport;
using Wolverine;

namespace GaWeCodes.Wolverine.Messaging.Transport;

public interface IWolverineMessagingTransport : IMessagingTransportAdapter
{
    void Configure(WolverineOptions options, bool provisionInfrastructure);

    void ConfigureSubscription(WolverineOptions options, IntegrationEventSubscription subscription);
}
