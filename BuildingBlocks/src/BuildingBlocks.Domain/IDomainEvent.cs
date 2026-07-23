namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a domain event that occurs within the domain model.
/// </summary>
/// <remarks>
/// Domain events communicate that something meaningful has already happened in the domain, decoupling the code that
/// produces a change from the code that reacts to it. Model them as immutable records that carry the facts of what
/// occurred, and raise them from aggregates so that side effects can be dispatched after the aggregate is persisted.
/// </remarks>
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
