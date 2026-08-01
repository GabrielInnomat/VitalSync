using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Tracks the state-stored aggregates loaded or added within the current unit of work.
/// </summary>
/// <remarks>
/// EF Core tracks the aggregate's <b>state</b> object, not the aggregate itself (see <see cref="IStateOwner"/>), so the
/// change tracker cannot answer "which aggregates took part in this command". The repository registers every aggregate
/// it hands out here instead — the same arrangement the event-sourced path already uses
/// (<see cref="MartenAggregateTracker"/>), which keeps both persistence styles symmetric. At commit the unit of work
/// copies each entry's current state onto its tracked persistence entity, enrols the uncommitted domain events in the
/// outbox, and clears them afterwards. The tracker is scoped: one instance per command dispatch.
/// </remarks>
public sealed class EfCoreAggregateTracker
{
    private readonly List<TrackedStateAggregate> _entries = [];

    /// <summary>
    /// Gets the aggregates tracked in the current unit of work, in the order they were first tracked.
    /// </summary>
    public IReadOnlyList<TrackedStateAggregate> Entries => _entries;

    /// <summary>
    /// Registers an aggregate together with the persistence entity EF Core tracks for it.
    /// </summary>
    /// <remarks>
    /// Tracking is idempotent per aggregate instance, so an aggregate that is both loaded and explicitly added is
    /// registered only once.
    /// </remarks>
    /// <param name="aggregate">The aggregate to track until commit.</param>
    /// <param name="persistedState">The state instance EF Core tracks, whose values are refreshed from the aggregate at commit.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> or <paramref name="persistedState"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregate"/> does not expose its state.</exception>
    public void Track(IDomainEventsManager aggregate, object persistedState)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(persistedState);

        if (aggregate is not IStateOwner stateOwner)
        {
            throw new ArgumentException(
                $"The aggregate '{aggregate.GetType()}' does not expose its state and cannot be persisted by EF Core.",
                nameof(aggregate));
        }

        if (_entries.Exists(entry => ReferenceEquals(entry.Aggregate, aggregate)))
        {
            return;
        }

        _entries.Add(new TrackedStateAggregate(aggregate, stateOwner, persistedState));
    }

    /// <summary>
    /// Clears the domain events of all tracked aggregates and forgets them.
    /// </summary>
    /// <remarks>
    /// Call only after the context has saved, so events are never lost before they reach the outbox.
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

/// <summary>
/// An aggregate tracked by <see cref="EfCoreAggregateTracker"/> together with its persistence entity.
/// </summary>
/// <remarks>
/// <paramref name="PersistedState"/> is the instance EF Core knows; because state objects are immutable, every applied
/// event replaces the aggregate's state, so the tracked instance goes stale and is refreshed from
/// <paramref name="StateOwner"/> at commit time.
/// </remarks>
/// <param name="Aggregate">The aggregate whose uncommitted domain events are dispatched at commit.</param>
/// <param name="StateOwner">The same aggregate, viewed through its state accessor.</param>
/// <param name="PersistedState">The state instance tracked by EF Core.</param>
public sealed record TrackedStateAggregate(
    IDomainEventsManager Aggregate,
    IStateOwner StateOwner,
    object PersistedState);
