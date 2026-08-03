namespace BuildingBlocks.Domain;

/// <summary>
/// Grants the persistence layer the ability to create the empty aggregate hull it reconstitutes a stored aggregate into.
/// </summary>
/// <remarks>
/// Reconstitution is not creation: a repository does not author a new aggregate, it rebuilds one that already exists —
/// either by restoring its persisted state (<see cref="IStateOwner.Restore"/>) or by replaying its event history
/// (<see cref="IEventSourcedAggregateRoot{TKey}.LoadFromHistory"/>). Both need an instance before they have anything to
/// fold into, which is what <see cref="CreateEmpty"/> supplies.
/// <para>
/// Implement it <b>explicitly</b> and keep the aggregate's parameterless constructor private, exactly as
/// <see cref="IStateOwner"/> and <see cref="IDomainEventOwner"/> are implemented. A static abstract member is callable
/// only through a type parameter constrained to this interface, so an explicit implementation leaves the aggregate
/// with no publicly reachable empty constructor: <c>new Widget()</c> and <c>Widget.CreateEmpty()</c> are both
/// compile errors, and the aggregate's own factory method stays the only way to bring one into existence. The
/// requirement is expressed as a constraint on <c>IRepository</c>, so an aggregate that does not satisfy it fails to
/// compile at the injection site rather than at the first load.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The type of the aggregate root that is reconstituted.</typeparam>
public interface IReconstitutable<TSelf>
    where TSelf : IReconstitutable<TSelf>
{
    /// <summary>
    /// Creates an aggregate instance carrying the empty state, ready to be reconstituted.
    /// </summary>
    /// <remarks>
    /// Called by a repository immediately before it restores a persisted state or replays an event history into the
    /// instance. The returned aggregate is not yet identified — it has raised no events and its state is the empty one
    /// — so it must never be handed to domain code in this condition.
    /// </remarks>
    /// <returns>An unidentified aggregate instance holding the empty state.</returns>
    static abstract TSelf CreateEmpty();
}
