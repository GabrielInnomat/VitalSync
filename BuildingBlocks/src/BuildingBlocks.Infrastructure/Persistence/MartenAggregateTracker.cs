using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Tracks the event-sourced aggregates loaded or added within the current unit of work.
/// </summary>
/// <remarks>
/// Marten sessions do not track our aggregates, so the event-sourced repository registers every aggregate it hands
/// out (loaded or newly added) here — mirroring EF Core's change tracker. At commit, the Marten unit of work appends
/// each tracked aggregate's uncommitted domain events to its stream, enrolls them in the outbox, and clears them
/// after the session has committed. The tracker is scoped: one instance per command dispatch.
/// </remarks>
public sealed class MartenAggregateTracker
{
    private readonly List<TrackedAggregate> _entries = [];

    /// <summary>
    /// Gets the aggregates tracked in the current unit of work, in the order they were first tracked.
    /// </summary>
    /// <remarks>
    /// The unit of work appends each entry's uncommitted <see cref="IHasDomainEvents.DomainEvents"/> to the stream
    /// identified by the entry, enrolls the events in the outbox before committing the session, and finally calls
    /// <see cref="ClearDomainEvents"/>.
    /// </remarks>
    public IReadOnlyList<TrackedAggregate> Entries => _entries;

    /// <summary>
    /// Registers an aggregate so its uncommitted events are appended and dispatched at commit.
    /// </summary>
    /// <remarks>
    /// Tracking is idempotent per aggregate instance: registering the same instance again is a no-op, so an aggregate
    /// that is both loaded and explicitly added is appended only once.
    /// </remarks>
    /// <param name="aggregate">The aggregate to track until commit.</param>
    /// <param name="streamKey">The accessor yielding the key of the event stream the aggregate's events belong to.</param>
    /// <param name="expectedVersion">The accessor yielding the expected stream version after the uncommitted events are appended.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Clears the domain events of all tracked aggregates and forgets them.
    /// </summary>
    /// <remarks>
    /// Call only after the session has committed, so events are never lost before they reach the outbox.
    /// </remarks>
    public void ClearDomainEvents()
    {
        foreach (var entry in _entries)
        {
            entry.Aggregate.ClearDomainEvents();
        }

        _entries.Clear();
    }
}
