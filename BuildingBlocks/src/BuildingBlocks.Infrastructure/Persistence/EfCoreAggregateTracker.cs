using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class EfCoreAggregateTracker
{
    private readonly List<TrackedStateAggregate> _entries = [];

    public IReadOnlyList<TrackedStateAggregate> Entries => _entries;

    public void Track(IDomainEventOwner aggregate, IStateOwner stateOwner, object persistedState)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(stateOwner);
        ArgumentNullException.ThrowIfNull(persistedState);

        if (_entries.Exists(entry => ReferenceEquals(entry.Aggregate, aggregate)))
        {
            return;
        }

        _entries.Add(new TrackedStateAggregate(aggregate, stateOwner, persistedState));
    }

    public void ClearDomainEvents()
    {
        foreach (var entry in _entries)
        {
            entry.Aggregate.ClearDomainEvents();
        }

        _entries.Clear();
    }
}

public sealed record TrackedStateAggregate(
    IDomainEventOwner Aggregate,
    IStateOwner StateOwner,
    object PersistedState);
