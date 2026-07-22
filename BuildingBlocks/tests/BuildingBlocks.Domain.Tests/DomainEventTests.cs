using BuildingBlocks.Domain.Tests.TestDoubles;

namespace BuildingBlocks.Domain.Tests;

public sealed class DomainEventTests
{
    [Fact]
    public void Constructor_AssignsNonEmptyEventId()
    {
        var @event = new TestDomainEvent(1);

        Assert.NotEqual(Guid.Empty, @event.EventId);
    }

    [Fact]
    public void Constructor_AssignsUniqueEventIds()
    {
        var first = new TestDomainEvent(1);
        var second = new TestDomainEvent(1);

        Assert.NotEqual(first.EventId, second.EventId);
    }

    [Fact]
    public void OccurredAt_DefaultsToUnsetSoStampingCanDetectIt()
    {
        var @event = new TestDomainEvent(1);

        Assert.Equal(0, @event.OccurredAt.Ticks);
        Assert.Equal(default, @event.OccurredAt);
    }

    [Fact]
    public void With_OverridesOccurredAtButKeepsEventId()
    {
        var original = new TestDomainEvent(1);
        var stampedAt = new DateTimeOffset(2026, 07, 22, 12, 00, 00, TimeSpan.Zero);

        var stamped = original with { OccurredAt = stampedAt };

        Assert.Equal(stampedAt, stamped.OccurredAt);
        Assert.Equal(original.EventId, stamped.EventId);
        Assert.Equal(0, original.OccurredAt.Ticks);
    }
}
