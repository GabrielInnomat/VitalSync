using BuildingBlocks.Domain.Tests.TestDoubles;

namespace BuildingBlocks.Domain.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "Need to check if the overwritten equality operator handles null correctly")]
public sealed class EventSourcedAggregateRootTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 07, 22, 10, 30, 00, TimeSpan.Zero);

    private static readonly FakeClock Clock = new(FixedNow);

    [Fact]
    public void RaiseEvent_AppliesStateAppendsEventAndIncrementsVersion()
    {
        var aggregate = new TestEventSourcedAggregate();

        aggregate.Raise(new TestDomainEvent(5), Clock);

        Assert.Equal(new TestId(5), aggregate.Id);
        Assert.Equal(5, aggregate.State.Value);
        Assert.Single(aggregate.DomainEvents);
        Assert.Equal(1, ((IEventSourcedAggregateRoot<TestId>)aggregate).Version);
    }

    [Fact]
    public void RaiseEvent_WithUnsetOccurredAt_StampsWithClockNow()
    {
        var aggregate = new TestEventSourcedAggregate();

        aggregate.Raise(new TestDomainEvent(5), Clock);

        var raised = Assert.IsType<TestDomainEvent>(aggregate.DomainEvents.Single());
        Assert.Equal(FixedNow, raised.OccurredAt);
    }

    [Fact]
    public void RaiseEvent_WithPresetOccurredAt_LeavesTimestampUnchanged()
    {
        var aggregate = new TestEventSourcedAggregate();
        var preset = new DateTimeOffset(2000, 01, 01, 00, 00, 00, TimeSpan.Zero);

        aggregate.Raise(new TestDomainEvent(5) { OccurredAt = preset }, Clock);

        var raised = Assert.IsType<TestDomainEvent>(aggregate.DomainEvents.Single());
        Assert.Equal(preset, raised.OccurredAt);
    }

    [Fact]
    public void RaiseEvent_WithNonDomainEventImplementation_IsNotStamped()
    {
        var aggregate = new TestEventSourcedAggregate();

        aggregate.Raise(new RawDomainEvent(5), Clock);

        var raised = Assert.IsType<RawDomainEvent>(aggregate.DomainEvents.Single());
        Assert.Equal(default, raised.OccurredAt);
    }

    [Fact]
    public void RaiseEvent_MultipleTimes_TracksVersionAndEvents()
    {
        var aggregate = new TestEventSourcedAggregate();

        aggregate.Raise(new TestDomainEvent(1), Clock);
        aggregate.Raise(new TestDomainEvent(2), Clock);
        aggregate.Raise(new TestDomainEvent(3), Clock);

        Assert.Equal(3, aggregate.DomainEvents.Count);
        Assert.Equal(3, ((IEventSourcedAggregateRoot<TestId>)aggregate).Version);
        Assert.Equal(3, aggregate.State.Value);
    }

    [Fact]
    public void RaiseEvent_WhenAppliedStateLeavesIdEmpty_ThrowsDomainValidationException()
    {
        var aggregate = new NeverIdentifiedAggregate();

        var ex = Assert.Throws<DomainValidationException>(
            () => aggregate.Raise(new TestDomainEvent(5), Clock));
        Assert.Equal(
            "The aggregate's identity must be set to a non-empty value by the applied event.",
            ex.Message);
    }

    [Fact]
    public void LoadFromHistory_ReplaysEventsAndSetsVersion()
    {
        var aggregate = new TestEventSourcedAggregate();
        var history = new IDomainEvent[]
        {
            new TestDomainEvent(1),
            new TestDomainEvent(2),
            new TestDomainEvent(3),
        };

        ((IEventSourcedAggregateRoot<TestId>)aggregate).LoadFromHistory(history);

        Assert.Equal(new TestId(3), aggregate.Id);
        Assert.Equal(3, aggregate.State.Value);
        Assert.Equal(3, ((IEventSourcedAggregateRoot<TestId>)aggregate).Version);
        // Replayed history must not be exposed as uncommitted events.
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void LoadFromHistory_AfterEventRaised_ThrowsInvalidOperationException()
    {
        var aggregate = new TestEventSourcedAggregate();
        aggregate.Raise(new TestDomainEvent(1), Clock);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ((IEventSourcedAggregateRoot<TestId>)aggregate)
                .LoadFromHistory([new TestDomainEvent(2)]));
        Assert.Equal(
            "LoadFromHistory cannot be called after events have been raised on the aggregate.",
            ex.Message);
    }

    [Fact]
    public void LoadFromHistory_ThenRaiseEvent_ContinuesVersioning()
    {
        var aggregate = new TestEventSourcedAggregate();
        ((IEventSourcedAggregateRoot<TestId>)aggregate)
            .LoadFromHistory([new TestDomainEvent(1), new TestDomainEvent(2)]);

        aggregate.Raise(new TestDomainEvent(3), Clock);

        Assert.Equal(3, ((IEventSourcedAggregateRoot<TestId>)aggregate).Version);
        Assert.Single(aggregate.DomainEvents);
        Assert.Equal(3, aggregate.State.Value);
    }

    [Fact]
    public void Equals_SameTypeSameId_AreEqual()
    {
        var a = new TestEventSourcedAggregate();
        var b = new TestEventSourcedAggregate();
        a.Raise(new TestDomainEvent(1), Clock);
        b.Raise(new TestDomainEvent(1), Clock);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_SameIdDifferentType_AreNotEqual()
    {
        var a = new TestEventSourcedAggregate();
        var b = new OtherEventSourcedAggregate();
        a.Raise(new TestDomainEvent(1), Clock);
        b.Raise(new TestDomainEvent(1), Clock);

        Assert.False(a.Equals(b as object));
    }

    [Fact]
    public void Equals_DifferentId_AreNotEqual()
    {
        var a = new TestEventSourcedAggregate();
        var b = new TestEventSourcedAggregate();
        a.Raise(new TestDomainEvent(1), Clock);
        b.Raise(new TestDomainEvent(2), Clock);

        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void EqualityOperator_BothNull_AreEqual()
    {
        TestEventSourcedAggregate? a = null;
        TestEventSourcedAggregate? b = null;

        Assert.True(a == b);
    }
}
