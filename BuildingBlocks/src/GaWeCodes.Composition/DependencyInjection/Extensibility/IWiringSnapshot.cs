using GaWeCodes.DependencyInjection.Wiring;
using GaWeCodes.Messaging.Transport;

namespace GaWeCodes.DependencyInjection.Extensibility;

public interface IWiringSnapshot
{
    bool RequiresRuntime { get; }

    bool PersistenceSelected { get; }

    bool ProvisionsInfrastructure { get; }

    IMessagingTransportAdapter? Transport { get; }

    IntegrationEventSubscription? Subscription { get; }

    bool IsTransientFault(Exception exception);
}
