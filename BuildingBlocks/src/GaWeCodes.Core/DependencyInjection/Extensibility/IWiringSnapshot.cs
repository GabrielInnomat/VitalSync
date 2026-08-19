using GaWeCodes.Core.DependencyInjection.Wiring;
using GaWeCodes.Core.Messaging.Transport;

namespace GaWeCodes.Core.DependencyInjection.Extensibility;

public interface IWiringSnapshot
{
    bool RequiresRuntime { get; }

    bool PersistenceSelected { get; }

    bool ProvisionsInfrastructure { get; }

    IMessagingTransportAdapter? Transport { get; }

    IntegrationEventSubscription? Subscription { get; }

    bool IsTransientFault(Exception exception);
}
