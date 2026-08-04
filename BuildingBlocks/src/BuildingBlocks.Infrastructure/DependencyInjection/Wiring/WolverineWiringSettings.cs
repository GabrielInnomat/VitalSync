namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal sealed class WolverineWiringSettings
{
    public PersistenceChoice Persistence { get; private set; } = PersistenceChoice.None;

    public MessagingSettings? Messaging { get; private set; }

    public IntegrationEventSubscription? Subscription { get; private set; }

    public bool RequiresWolverine =>
        Persistence.IsSelected || Messaging is not null || Subscription is not null;

    public void SelectPersistence(PersistenceChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);

        if (!Persistence.IsSelected || Persistence == choice)
        {
            Persistence = choice;
            return;
        }

        throw new InvalidOperationException(
            Persistence.GetType() == choice.GetType()
                ? $"{choice.Description} was called twice with different arguments. A bounded context has exactly " +
                    "one write database (ADR-0021), so the second call would silently point the aggregates and the " +
                    "outbox at different databases."
                : "Two persistence strategies were configured for the same host " +
                    $"({Persistence.Description} and {choice.Description}). " +
                    "A microservice hosts exactly one bounded context, and a bounded context uses exactly one " +
                    "persistence strategy (ADR-0019/0020/0021): state-stored via EF Core, or event-sourced via Marten. " +
                    "A commit cannot span both stores atomically because they live in separate databases (ADR-0020). " +
                    "A context that appears to need both is a sign it is cut wrong and should be split into two " +
                    "bounded contexts, each in its own microservice with its own single persistence strategy.");
    }

    public void SelectMessaging(MessagingSettings messaging) => Messaging = messaging;

    public void SelectSubscription(IntegrationEventSubscription subscription)
    {
        if (Subscription is not null)
        {
            throw new InvalidOperationException(
                "SubscribeToIntegrationEvents was called more than once. A microservice hosts exactly one bounded " +
                "context and owns exactly one queue (ADR-0023); bind every topic pattern it consumes to that one " +
                "queue in a single call instead.");
        }

        Subscription = subscription;
    }
}
