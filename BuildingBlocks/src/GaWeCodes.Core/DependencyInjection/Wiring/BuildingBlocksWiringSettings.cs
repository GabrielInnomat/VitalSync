using GaWeCodes.Core.DependencyInjection.Extensibility;
using GaWeCodes.Core.Messaging.Transport;

namespace GaWeCodes.Core.DependencyInjection.Wiring;

internal sealed class BuildingBlocksWiringSettings : IWiringSnapshot
{
    public PersistenceSelection Persistence { get; } = new();

    public MessagingSelection Messaging { get; } = new();

    public ProvisioningSelection Provisioning { get; } = new();

    public RuntimeActivation Runtime { get; } = new();

    public bool RequiresRuntime =>
        Persistence.IsSelected || Messaging.IsSelected || Messaging.Subscription is not null;

    public bool PersistenceSelected => Persistence.IsSelected;

    public bool ProvisionsInfrastructure => Provisioning.ProvisionsInfrastructure;

    public IMessagingTransportAdapter? Transport => Messaging.Transport;

    public IntegrationEventSubscription? Subscription => Messaging.Subscription;

    public bool IsTransientFault(Exception exception) => Persistence.IsTransientFault(exception);
}
