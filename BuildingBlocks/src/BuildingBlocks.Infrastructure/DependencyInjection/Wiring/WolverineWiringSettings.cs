namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal sealed class WolverineWiringSettings
{
    public bool ApplyDomainEventRouting { get; set; }

    public string? EfCoreMessageStoreConnectionString { get; set; }

    public bool MartenMessageStoreSelected { get; set; }

    public MessagingSettings? Messaging { get; set; }

    public IntegrationEventSubscription? Subscription { get; set; }

    public bool HasMessageStore =>
        EfCoreMessageStoreConnectionString is not null || MartenMessageStoreSelected;

    public bool RequiresWolverine =>
        ApplyDomainEventRouting
        || EfCoreMessageStoreConnectionString is not null
        || Messaging is not null
        || Subscription is not null;
}
