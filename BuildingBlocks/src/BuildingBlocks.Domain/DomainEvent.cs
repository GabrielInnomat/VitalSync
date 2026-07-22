namespace BuildingBlocks.Domain;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
    }

    public Guid EventId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}
