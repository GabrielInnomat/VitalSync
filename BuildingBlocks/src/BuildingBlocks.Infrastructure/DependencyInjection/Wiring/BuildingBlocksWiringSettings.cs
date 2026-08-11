namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal sealed class BuildingBlocksWiringSettings
{
    public PersistenceSelection Persistence { get; } = new();

    public MessagingSelection Messaging { get; } = new();

    public ProvisioningSelection Provisioning { get; } = new();

    public bool RequiresWolverine =>
        Persistence.IsSelected || Messaging.IsSelected || Messaging.Subscription is not null;
}
