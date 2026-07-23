namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an aggregate root that has domain events.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the read-only collection of domain events associated with the aggregate root.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
}
