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

    [Fact]
    public void Restore_BringsBackTheChildrenCarriedByTheState()
    {
        var aggregate = Reconstitute<ParentAggregate>();
        var persisted = new ParentState(new TestId(1), 0)
        {
            Version = 5,
            Children = new List<ChildState> { new(new TestId(2), 3) },
        };

        ((IStateOwner)aggregate).Restore(persisted);

        var child = Assert.Single(aggregate.Children);
        Assert.Equal(new TestId(2), child.Id);
        Assert.Equal(3, child.Value);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Restore_YieldsChildViewsThatStayLiveAgainstTheRoot()
    {
        var aggregate = Reconstitute<ParentAggregate>();
        ((IStateOwner)aggregate).Restore(new ParentState(new TestId(1), 0)
        {
            Version = 5,
            Children = new List<ChildState> { new(new TestId(2), 3) },
        });

        aggregate.Child(new TestId(2)).ChangeValue(9);

        Assert.Equal(9, aggregate.Child(new TestId(2)).Value);
        Assert.Equal(6, ((IStateOwner)aggregate).Version);
    }

    private static TAggregate Reconstitute<TAggregate>()
        where TAggregate : class =>
        (TAggregate)Activator.CreateInstance(typeof(TAggregate), nonPublic: true)!;
}
