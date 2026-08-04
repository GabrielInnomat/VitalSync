using BuildingBlocks.Domain.Tests.TestDoubles;

namespace BuildingBlocks.Domain.Tests;

public sealed class ReconstitutableTests
{
    [Fact]
    public void CreateEmpty_ReturnsAnUnidentifiedAggregateWithoutEvents()
    {
        var aggregate = Reconstitute<ReconstitutableAggregate>();

        Assert.True(aggregate.Id.IsEmpty);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void CreateEmpty_ReturnsADistinctInstanceEachTime()
    {
        var first = Reconstitute<ReconstitutableAggregate>();
        var second = Reconstitute<ReconstitutableAggregate>();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void CreateEmpty_YieldsAHullThatRestoresPersistedState()
    {
        var aggregate = Reconstitute<ReconstitutableAggregate>();
        var persisted = new TestState(new TestId(42), 42);

        ((IStateOwner)aggregate).Restore(persisted);

        Assert.Equal(new TestId(42), aggregate.Id);
        Assert.Equal(42, aggregate.CurrentState.Value);

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void CreateEmpty_YieldsAHullThatReplaysHistory()
    {
        var aggregate = Reconstitute<ReconstitutableEventSourcedAggregate>();

        ((IEventSourcedAggregateRoot<TestId>)aggregate)
            .LoadFromHistory([new TestDomainEvent(1), new TestDomainEvent(2)]);

        Assert.Equal(new TestId(2), aggregate.Id);
        Assert.Equal(2, ((IEventSourcedAggregateRoot<TestId>)aggregate).Version);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void NamedFactory_ProducesAnIdentifiedAggregateThatRecordedItsEvent()
    {
        var aggregate = ReconstitutableAggregate.Create(7);

        Assert.Equal(new TestId(7), aggregate.Id);
        Assert.Single(aggregate.DomainEvents);
    }

    private static TAggregate Reconstitute<TAggregate>()
        where TAggregate : class =>
        (TAggregate)Activator.CreateInstance(typeof(TAggregate), nonPublic: true)!;
}
