namespace BuildingBlocks.Domain.Tests.TestDoubles;

internal sealed record TestDomainEvent(int NewValue) : DomainEvent;

internal sealed class RawDomainEvent(int newValue) : IDomainEvent
{
    public int NewValue { get; } = newValue;

    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; }
}
