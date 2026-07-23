namespace BuildingBlocks.Domain;

/// <summary>
/// Represents a base class for domain events in the domain model. A domain event is an occurrence that is significant to the business domain and is used to communicate changes or actions that have taken place within the system. This class provides a common implementation for domain events, including a unique identifier and a timestamp indicating when the event occurred.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class. The constructor generates a new unique identifier for the event.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
    }

    /// <summary>
    /// Gets the unique identifier for the domain event. This identifier is generated when the event is created and can be used to track and reference the event within the system.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Gets the timestamp indicating when the domain event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }
}
