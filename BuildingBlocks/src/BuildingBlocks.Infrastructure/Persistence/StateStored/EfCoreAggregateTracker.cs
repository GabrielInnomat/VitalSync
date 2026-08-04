using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence.StateStored;

internal sealed class EfCoreAggregateTracker
{
    private readonly List<TrackedStateAggregate> _entries = [];

    public IReadOnlyList<TrackedStateAggregate> Entries => _entries;

    public void Track(
        IDomainEventOwner aggregate,
        IStateOwner stateOwner,
        object persistedState,
        string aggregateName,
        string aggregateId)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(stateOwner);
        ArgumentNullException.ThrowIfNull(persistedState);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        if (_entries.Exists(entry => ReferenceEquals(entry.Aggregate, aggregate)))
        {
            return;
        }

        _entries.Add(new TrackedStateAggregate(aggregate, stateOwner, persistedState, aggregateName, aggregateId));
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
