using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class MartenAggregateTracker
{
    private readonly List<TrackedAggregate> _entries = [];

    public IReadOnlyList<TrackedAggregate> Entries => _entries;

    public void Track(IDomainEventOwner aggregate, Func<string> streamKey, Func<long> expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(streamKey);
        ArgumentNullException.ThrowIfNull(expectedVersion);

        if (_entries.Exists(entry => ReferenceEquals(entry.Aggregate, aggregate)))
        {
            return;
        }

        _entries.Add(new TrackedAggregate(aggregate, streamKey, expectedVersion));
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
