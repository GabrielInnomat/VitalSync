namespace BuildingBlocks.Infrastructure.Messaging;

internal sealed class WolverineWiringSettings
{
    public bool ApplyDomainEventRouting { get; set; }

    public string? EfCoreMessageStoreConnectionString { get; set; }

    public Uri? RabbitMqUri { get; set; }

    public IntegrationEventSubscription? Subscription { get; set; }

    public bool RequiresWolverine =>
        ApplyDomainEventRouting
        || EfCoreMessageStoreConnectionString is not null
        || RabbitMqUri is not null
        || Subscription is not null;
}
