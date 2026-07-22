namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>A concrete <see cref="DomainEvent"/> record used by the tests.</summary>
public sealed record TestDomainEvent(int NewValue) : DomainEvent;

/// <summary>
/// A raw <see cref="IDomainEvent"/> that is NOT a <see cref="DomainEvent"/>. Used to
/// prove the event-sourced aggregate only stamps <see cref="DomainEvent"/> records.
/// </summary>
public sealed class RawDomainEvent(int newValue) : IDomainEvent
{
    public int NewValue { get; } = newValue;

    public Guid EventId { get; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; }
}
