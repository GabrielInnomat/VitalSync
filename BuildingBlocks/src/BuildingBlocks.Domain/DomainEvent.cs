namespace BuildingBlocks.Domain;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent(IClock clock)
    {
        EventId = Guid.NewGuid();
        OccurredAt = clock.Now;
    }

    public Guid EventId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}