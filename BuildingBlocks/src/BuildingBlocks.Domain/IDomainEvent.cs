namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a domain event that occurs within the domain model.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the domain event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp indicating when the domain event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
