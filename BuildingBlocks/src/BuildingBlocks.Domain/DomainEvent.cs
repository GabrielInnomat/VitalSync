namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for domain events, occurrences that are significant to the business domain.
/// </summary>
/// <remarks>
/// Deriving from this record gives events a generated <see cref="EventId"/> and value-based equality for free, so
/// concrete events only need to declare the facts they carry. Model each event as an immutable record of something
/// that has already happened, and let the raising aggregate stamp <see cref="OccurredAt"/>.
/// </remarks>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class, generating a new unique identifier for the event.
    /// </summary>
    /// <remarks>
    /// The identifier is assigned at construction so every event is uniquely traceable from the moment it is created,
    /// before it has been persisted or dispatched.
    /// </remarks>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
    }

    /// <inheritdoc/>
    public Guid EventId { get; init; }

    /// <inheritdoc/>
    public DateTimeOffset OccurredAt { get; init; }
}
