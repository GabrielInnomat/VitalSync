using BuildingBlocks.Domain.Tests.TestDoubles;

namespace BuildingBlocks.Domain.Tests;

// TODO-10 / ADR-0025 reconstitution amendment: rehydration goes through the aggregate's own explicitly
// implemented static factory instead of a new() constraint or Activator.CreateInstance. What is asserted here
// is the behavior a repository relies on; that `new ReconstitutableAggregate()` and
// `ReconstitutableAggregate.CreateEmpty()` do not compile is enforced by the compiler, not by a test - the
// generic helper below is deliberately the only way these tests can reach an empty hull either.
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
        // The state-stored path: create the hull, then restore the state the repository loaded into it.
        var aggregate = Reconstitute<ReconstitutableAggregate>();
        var persisted = new TestState(new TestId(42), 42);

        ((IStateOwner)aggregate).Restore(persisted);

        Assert.Equal(new TestId(42), aggregate.Id);
        Assert.Equal(42, aggregate.CurrentState.Value);

        // Restoring is not a state change in the domain sense, so it raises nothing.
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void CreateEmpty_YieldsAHullThatReplaysHistory()
    {
        // The event-sourced path, written identically - that symmetry is the point of ADR-0025.
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
        // The public route, unaffected by reconstitution: creation still goes through the domain.
        var aggregate = ReconstitutableAggregate.Create(7);

        Assert.Equal(new TestId(7), aggregate.Id);
        Assert.Single(aggregate.DomainEvents);
    }

    // Mirrors how a repository reaches CreateEmpty: only through a constrained type parameter.
    private static TAggregate Reconstitute<TAggregate>()
        where TAggregate : IReconstitutable<TAggregate> => TAggregate.CreateEmpty();
}
