using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Persistence;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class EfCoreAggregateTrackerTests
{
    [Fact]
    public void Track_RecordsTheAggregateWithItsStateAccessorAndPersistedState()
    {
        var tracker = new EfCoreAggregateTracker();
        var probe = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));
        var stateOwner = (IStateOwner)probe;
        var persisted = stateOwner.State;

        tracker.Track(probe, stateOwner, persisted);

        var entry = Assert.Single(tracker.Entries);
        Assert.Same(probe, entry.Aggregate);
        Assert.Same(stateOwner, entry.StateOwner);
        Assert.Same(persisted, entry.PersistedState);
    }

    [Fact]
    public void Track_SameAggregateTwice_RegistersItOnce()
    {
        var tracker = new EfCoreAggregateTracker();
        var probe = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));
        var stateOwner = (IStateOwner)probe;

        tracker.Track(probe, stateOwner, stateOwner.State);
        tracker.Track(probe, stateOwner, stateOwner.State);

        Assert.Single(tracker.Entries);
    }

    [Fact]
    public void Track_DistinctAggregates_KeepsInsertionOrder()
    {
        var tracker = new EfCoreAggregateTracker();
        var first = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));
        var second = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));

        tracker.Track(first, (IStateOwner)first, ((IStateOwner)first).State);
        tracker.Track(second, (IStateOwner)second, ((IStateOwner)second).State);

        Assert.Equal([first, second], tracker.Entries.Select(entry => entry.Aggregate));
    }

    [Fact]
    public void Track_NullArgument_ThrowsArgumentNullException()
    {
        var tracker = new EfCoreAggregateTracker();
        var probe = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));
        var stateOwner = (IStateOwner)probe;

        Assert.Throws<ArgumentNullException>(() => tracker.Track(null!, stateOwner, stateOwner.State));
        Assert.Throws<ArgumentNullException>(() => tracker.Track(probe, null!, stateOwner.State));
        Assert.Throws<ArgumentNullException>(() => tracker.Track(probe, stateOwner, null!));
    }

    [Fact]
    public void ClearDomainEvents_ClearsTheAggregatesAndForgetsThem()
    {
        var tracker = new EfCoreAggregateTracker();
        var probe = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));
        var stateOwner = (IStateOwner)probe;
        tracker.Track(probe, stateOwner, stateOwner.State);

        Assert.NotEmpty(probe.DomainEvents);

        tracker.ClearDomainEvents();

        Assert.Empty(probe.DomainEvents);
        Assert.Empty(tracker.Entries);
    }

    [Fact]
    public void StateAccessor_ReflectsTheCurrentStateAfterAnEventWasRaised()
    {
        var tracker = new EfCoreAggregateTracker();
        var probe = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));
        var stateOwner = (IStateOwner)probe;
        var persisted = stateOwner.State;
        tracker.Track(probe, stateOwner, persisted);

        probe.Rename("renamed");

        var entry = Assert.Single(tracker.Entries);
        Assert.NotSame(persisted, entry.StateOwner.State);
        Assert.Equal("renamed", Assert.IsType<FlushProbeState>(entry.StateOwner.State).Name);
    }
}
