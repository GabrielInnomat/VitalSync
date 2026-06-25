using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.Persistence;

/// <summary>
/// Provides read-only access to the domain events currently tracked by the
/// persistence context, and the ability to clear them after dispatch.
/// </summary>
public interface IDomainEventsAccessor
{
    /// <summary>Returns all domain events recorded by tracked aggregates.</summary>
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();

    /// <summary>Clears the domain events from all tracked aggregates.</summary>
    void ClearDomainEvents();
}