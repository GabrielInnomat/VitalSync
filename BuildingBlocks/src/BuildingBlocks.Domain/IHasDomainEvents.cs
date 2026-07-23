namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a domain object that exposes the domain events it has raised.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the read-only collection of domain events raised by the domain object.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
}
