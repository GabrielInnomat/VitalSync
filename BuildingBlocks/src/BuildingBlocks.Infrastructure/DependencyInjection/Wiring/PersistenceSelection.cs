using BuildingBlocks.Infrastructure.Persistence;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal sealed class PersistenceSelection
{
    private readonly List<IOutboxDurabilityConfigurator> _outboxDurability = [];

    public PersistenceChoice Choice { get; private set; } = PersistenceChoice.None;

    public bool IsChosen => Choice.IsChosen;

    public bool IsSelected => Choice.IsSelected;

    public bool IsDeliberatelyWithoutPersistence => Choice.IsDeliberatelyWithoutPersistence;

    public string? WriteConnectionString => Choice.WriteConnectionString;

    public IReadOnlyList<IOutboxDurabilityConfigurator> OutboxDurability => _outboxDurability;

    public void AddOutboxDurability(IOutboxDurabilityConfigurator configurator) =>
        _outboxDurability.Add(configurator);

    public void Select(PersistenceChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);

        if (!Choice.IsChosen || Choice == choice)
        {
            Choice = choice;
            return;
        }

        if (Choice.IsDeliberatelyWithoutPersistence || choice.IsDeliberatelyWithoutPersistence)
        {
            throw new InvalidOperationException(
                $"UseNoPersistence was combined with {(choice.IsDeliberatelyWithoutPersistence ? Choice : choice).Description}. " +
                "UseNoPersistence states that this host deliberately commits nothing, so it cannot be combined " +
                "with a persistence strategy. Keep exactly one of the two.");
        }

        throw new InvalidOperationException(
            Choice.Adapter?.GetType() == choice.Adapter?.GetType()
                ? $"{choice.Description} was called twice with different arguments. A bounded context has exactly " +
                    "one write database, so the second call would silently point the aggregates and the " +
                    "outbox at different databases."
                : "Two persistence strategies were configured for the same host " +
                    $"({Choice.Description} and {choice.Description}). " +
                    "A microservice hosts exactly one bounded context, and a bounded context uses exactly one " +
                    "persistence strategy: state-stored via EF Core, or event-sourced via Marten. " +
                    "A commit cannot span both stores atomically because they live in separate databases. " +
                    "A context that appears to need both is a sign it is cut wrong and should be split into two " +
                    "bounded contexts, each in its own microservice with its own single persistence strategy.");
    }
}
