using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Messaging.Transport;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Extensibility;

public interface IWiringSnapshot
{
    bool RequiresRuntime { get; }

    bool PersistenceSelected { get; }

    bool ProvisionsInfrastructure { get; }

    IMessagingTransportAdapter? Transport { get; }

    IntegrationEventSubscription? Subscription { get; }

    bool IsTransientFault(Exception exception);
}
