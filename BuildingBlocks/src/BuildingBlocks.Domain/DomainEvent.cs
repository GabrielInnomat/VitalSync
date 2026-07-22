namespace BuildingBlocks.Domain;

/// <summary>
/// Base for domain events. Events are pure data: they do not depend on a clock or
/// any infrastructure. <see cref="OccurredAt"/> is stamped by the aggregate at the
/// moment the event is raised (see <c>EventSourcedAggregateRoot.RaiseEvent</c>),
/// keeping event construction free of ambient dependencies.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
    }

    public Guid EventId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}
