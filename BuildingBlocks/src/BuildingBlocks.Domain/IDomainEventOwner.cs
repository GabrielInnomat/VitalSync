namespace BuildingBlocks.Domain;

/// <summary>
/// Grants the persistence layer authority over the lifecycle of the domain events an aggregate root owns.
/// </summary>
/// <remarks>
/// The aggregate owns its domain events (ADR-0006); this is the privileged view through which infrastructure — and
/// only infrastructure — may end their lifecycle once they have been dispatched. Implemented <b>explicitly</b> by
/// <see cref="AggregateRoot{TKey, TState}"/>, exactly like <see cref="IStateOwner"/>, so the members are not part of
/// the aggregate's public surface; cast the aggregate root to this interface to reach
/// <see cref="ClearDomainEvents"/>. Read the events through <see cref="IHasDomainEvents.DomainEvents"/> before saving
/// and clear them only afterwards.
/// </remarks>
public interface IDomainEventOwner : IHasDomainEvents
{
    /// <summary>
    /// Clears the domain events associated with the aggregate root.
    /// </summary>
    /// <remarks>
    /// This method is intended to be called after the aggregate root has been successfully persisted, in order to
    /// clear the domain events that have already been dispatched.
    /// </remarks>
    void ClearDomainEvents();
}
