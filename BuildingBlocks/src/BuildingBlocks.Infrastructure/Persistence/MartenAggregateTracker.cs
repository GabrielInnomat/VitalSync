using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Tracks the event-sourced aggregates saved within the current unit of work.
/// </summary>
/// <remarks>
/// Marten sessions do not track our aggregates, so the event-sourced repository registers every saved aggregate here
/// and the Marten unit of work clears their domain events after the session has committed — mirroring what the EF Core
/// unit of work derives from the change tracker. The tracker is scoped: one instance per command dispatch.
/// </remarks>
public sealed class MartenAggregateTracker
{
    private readonly List<IDomainEventsManager> _aggregates = [];

    /// <summary>
    /// Gets the aggregates saved in the current unit of work, in the order they were tracked.
    /// </summary>
    /// <remarks>
    /// The unit of work reads each aggregate's uncommitted <see cref="IHasDomainEvents.DomainEvents"/> from here to
    /// enroll them in the outbox before committing the session, then calls <see cref="ClearDomainEvents"/>.
    /// </remarks>
    public IReadOnlyList<IDomainEventsManager> Aggregates => _aggregates;

    /// <summary>
    /// Registers an aggregate whose uncommitted events were appended in the current unit of work.
    /// </summary>
    /// <param name="aggregate">The aggregate to track until commit.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is <see langword="null"/>.</exception>
    public void Track(IDomainEventsManager aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        _aggregates.Add(aggregate);
    }

    /// <summary>
    /// Clears the domain events of all tracked aggregates and forgets them.
    /// </summary>
    /// <remarks>
    /// Call only after the session has committed, so events are never lost before they reach the outbox.
    /// </remarks>
    public void ClearDomainEvents()
    {
        foreach (var aggregate in _aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        _aggregates.Clear();
    }
}
