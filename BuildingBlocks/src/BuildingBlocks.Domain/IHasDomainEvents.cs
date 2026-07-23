namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a domain object that exposes the domain events it has raised.
/// </summary>
/// <remarks>
/// This contract lets infrastructure collect the events an aggregate produced without depending on its concrete type,
/// so they can be dispatched once the aggregate has been persisted. Read the exposed events after saving; use
/// <see cref="IDomainEventsManager"/> to clear them once they have been dispatched.
/// </remarks>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the read-only collection of domain events raised by the domain object.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
}
