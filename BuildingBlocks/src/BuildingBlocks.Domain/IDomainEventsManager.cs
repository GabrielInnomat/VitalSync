namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a manager for domain events, providing functionality to manage and clear domain events associated with an aggregate root.
/// It is explicitly implemented by aggregate roots to ensure that they can manage their own domain events.
/// So you need to cast the aggregate root to this interface to access the ClearDomainEvents method. It is intended to be used in the persistence layer after successfull saving of the aggregate root to clear the domain events.
/// </summary>
public interface IDomainEventsManager : IHasDomainEvents
{
    /// <summary>
    /// Clears the domain events associated with the aggregate root. This method is intended to be called after the aggregate root has been successfully persisted to clear the domain events that have already been handled.
    /// </summary>
    void ClearDomainEvents();
}
