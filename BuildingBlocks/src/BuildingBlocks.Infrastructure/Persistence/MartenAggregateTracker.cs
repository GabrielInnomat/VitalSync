using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class MartenAggregateTracker
{
    private readonly List<TrackedAggregate> _entries = [];

    public IReadOnlyList<TrackedAggregate> Entries => _entries;

    public void Track(IDomainEventOwner aggregate, string aggregateName, string aggregateId, Func<long> version)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentNullException.ThrowIfNull(version);

        if (_entries.Exists(entry => ReferenceEquals(entry.Aggregate, aggregate)))
        {
            return;
        }

        _entries.Add(new TrackedAggregate(aggregate, aggregateName, aggregateId, version));
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
