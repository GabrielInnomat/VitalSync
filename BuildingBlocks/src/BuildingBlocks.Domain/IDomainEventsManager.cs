namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a manager for domain events, providing functionality to clear the domain events associated with an aggregate root.
/// </summary>
/// <remarks>
/// This interface is explicitly implemented by aggregate roots so that its members are not part of the aggregate's
/// public surface. Cast the aggregate root to this interface to access <see cref="ClearDomainEvents"/>. It is intended
/// to be used in the persistence layer after the aggregate root has been successfully saved.
/// </remarks>
public interface IDomainEventsManager : IHasDomainEvents
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
