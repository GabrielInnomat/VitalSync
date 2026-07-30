using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Events;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class DomainEventStamperTests
{
    private static readonly DateTimeOffset CommitTime = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Stamp_UnsetEvent_SetsOccurredAtToCommitTime()
    {
        var stamped = DomainEventStamper.Stamp(new SampleEvent(), CommitTime);

        Assert.Equal(CommitTime, stamped.OccurredAt);
    }

    [Fact]
    public void Stamp_UnsetEvent_KeepsEventId()
    {
        var original = new SampleEvent();

        var stamped = DomainEventStamper.Stamp(original, CommitTime);

        Assert.Equal(original.EventId, stamped.EventId);
    }

    [Fact]
    public void Stamp_AlreadyStampedEvent_IsLeftUnchanged()
    {
        var alreadyStamped = new SampleEvent { OccurredAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) };

        var result = DomainEventStamper.Stamp(alreadyStamped, CommitTime);

        Assert.Same(alreadyStamped, result);
        Assert.Equal(alreadyStamped.OccurredAt, result.OccurredAt);
    }

    [Fact]
    public void Stamp_NonDomainEventImplementation_IsReturnedUnchanged()
    {
        var custom = new CustomDomainEvent();

        var result = DomainEventStamper.Stamp(custom, CommitTime);

        Assert.Same(custom, result);
        Assert.Equal(0, result.OccurredAt.Ticks);
    }

    private sealed record SampleEvent : DomainEvent;

    private sealed class CustomDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTimeOffset OccurredAt { get; }
    }
}
