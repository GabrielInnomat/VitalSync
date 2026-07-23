namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for domain events, occurrences that are significant to the business domain.
/// </summary>
/// <remarks>
/// A domain event is used to communicate changes or actions that have taken place within the domain model.
/// </remarks>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class, generating a new unique identifier for the event.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
    }

    /// <summary>
    /// Gets the unique identifier of the domain event.
    /// </summary>
    /// <remarks>
    /// This identifier is generated when the event is created and can be used to track and reference the event within the system.
    /// </remarks>
    public Guid EventId { get; init; }

    /// <summary>
    /// Gets the timestamp indicating when the domain event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }
}
