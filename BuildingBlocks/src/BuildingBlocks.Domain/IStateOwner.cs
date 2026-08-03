namespace BuildingBlocks.Domain;

/// <summary>
/// Grants the persistence layer access to an aggregate root's state object.
/// </summary>
/// <remarks>
/// The state object is the aggregate's data — an immutable record carrying the identity (ADR-0010) — while the
/// aggregate itself is behavior. A state-stored repository therefore persists the <b>state</b> as its entity type and
/// rehydrates the aggregate around a loaded state, instead of trying to map the aggregate: the aggregate's
/// <c>Id</c> is derived from its state and has no setter, so it cannot serve as a mapped primary key. Implemented
/// <b>explicitly</b> by <see cref="AggregateRoot{TKey, TState}"/>, exactly like
/// <see cref="IDomainEventOwner"/>, so domain code never sees these members and cannot bypass the event fold by
/// restoring a state by hand. Only <c>BuildingBlocks.Infrastructure</c> consumes it.
/// </remarks>
public interface IStateOwner
{
    /// <summary>
    /// Gets the type of the aggregate root's state object.
    /// </summary>
    /// <remarks>
    /// The persistence layer maps this type — not the aggregate — so it needs the type to resolve the matching set
    /// on the write-database context.
    /// </remarks>
    Type StateType { get; }

    /// <summary>
    /// Gets the aggregate root's current state object.
    /// </summary>
    /// <remarks>
    /// Read at commit time to copy the current values onto the tracked persistence entity: the state is immutable, so
    /// every applied event replaces the instance and the originally tracked one would otherwise be stale.
    /// </remarks>
    object State { get; }

    /// <summary>
    /// Restores the aggregate root's state from a previously persisted state object.
    /// </summary>
    /// <remarks>
    /// Called by the repository immediately after constructing an empty aggregate, so the aggregate resumes from the
    /// persisted state without replaying events. This is not a state change in the domain sense and therefore raises
    /// no domain event.
    /// </remarks>
    /// <param name="state">The persisted state object to restore.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="state"/> is not of <see cref="StateType"/>.</exception>
    void Restore(object state);
}
