using BuildingBlocks.Domain.Tests.TestDoubles;

namespace BuildingBlocks.Domain.Tests;

public sealed class AggregateVersionTests
{
    [Fact]
    public void AFreshAggregate_StartsAtVersionZero()
    {
        var aggregate = new TestAggregate(new TestId(1));

        Assert.Equal(0, ((IStateOwner)aggregate).Version);
    }

    [Fact]
    public void AStateStoredAggregate_CountsItsEventsInTheStateVersion()
    {
        var aggregate = new TestAggregate(new TestId(1));

        aggregate.Raise(new TestDomainEvent(1));
        aggregate.Raise(new TestDomainEvent(2));

        Assert.Equal(2, ((IStateOwner)aggregate).Version);
        Assert.Equal(2, aggregate.CurrentState.Version);
    }

    [Fact]
    public void RestoringAState_AdoptsTheVersionThatWasPersistedWithIt()
    {
        var aggregate = new TestAggregate(new TestId(1));

        ((IStateOwner)aggregate).Restore(new TestState(new TestId(7), 7) { Version = 42 });

        Assert.Equal(42, ((IStateOwner)aggregate).Version);
    }

    [Fact]
    public void RaisingAfterRestore_ContinuesFromThePersistedVersion()
    {
        var aggregate = new TestAggregate(new TestId(1));
        ((IStateOwner)aggregate).Restore(new TestState(new TestId(7), 7) { Version = 42 });

        aggregate.Raise(new TestDomainEvent(8));

        Assert.Equal(43, ((IStateOwner)aggregate).Version);
    }

    [Fact]
    public void AnEventTheStateIgnores_StillAdvancesTheVersion()
    {
        var aggregate = new TestAggregate(new TestId(1));
        aggregate.Raise(new TestDomainEvent(5));

        aggregate.Raise(new IgnoredDomainEvent());

        Assert.Equal(5, aggregate.CurrentState.Value);
        Assert.Equal(2, ((IStateOwner)aggregate).Version);
    }
}
