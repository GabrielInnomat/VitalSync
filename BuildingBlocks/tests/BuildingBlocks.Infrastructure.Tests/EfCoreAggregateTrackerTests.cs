using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Persistence.StateStored;
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

        tracker.Track(probe, stateOwner, persisted, "flush-probe", probe.Id.Value.ToString());

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

        tracker.Track(probe, stateOwner, stateOwner.State, "flush-probe", probe.Id.Value.ToString());
        tracker.Track(probe, stateOwner, stateOwner.State, "flush-probe", probe.Id.Value.ToString());

        Assert.Single(tracker.Entries);
    }

    [Fact]
    public void Track_DistinctAggregates_KeepsInsertionOrder()
    {
        var tracker = new EfCoreAggregateTracker();
        var first = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));
        var second = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));

        tracker.Track(first, (IStateOwner)first, ((IStateOwner)first).State, "flush-probe", first.Id.Value.ToString());
        tracker.Track(second, (IStateOwner)second, ((IStateOwner)second).State, "flush-probe", second.Id.Value.ToString());

        Assert.Equal([first, second], tracker.Entries.Select(entry => entry.Aggregate));
    }

    [Fact]
    public void Track_NullArgument_ThrowsArgumentNullException()
    {
        var tracker = new EfCoreAggregateTracker();
        var probe = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));
        var stateOwner = (IStateOwner)probe;

        Assert.Throws<ArgumentNullException>(() => tracker.Track(null!, stateOwner, stateOwner.State, "flush-probe", "1"));
        Assert.Throws<ArgumentNullException>(() => tracker.Track(probe, null!, stateOwner.State, "flush-probe", "1"));
        Assert.Throws<ArgumentNullException>(() => tracker.Track(probe, stateOwner, null!, "flush-probe", "1"));
    }

    [Fact]
    public void ClearDomainEvents_ClearsTheAggregatesAndForgetsThem()
    {
        var tracker = new EfCoreAggregateTracker();
        var probe = FlushProbe.Create(new FlushProbeId(Guid.NewGuid()));
        var stateOwner = (IStateOwner)probe;
        tracker.Track(probe, stateOwner, stateOwner.State, "flush-probe", probe.Id.Value.ToString());

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
        tracker.Track(probe, stateOwner, persisted, "flush-probe", probe.Id.Value.ToString());

        probe.Rename("renamed");

        var entry = Assert.Single(tracker.Entries);
        Assert.NotSame(persisted, entry.StateOwner.State);
        Assert.Equal("renamed", Assert.IsType<FlushProbeState>(entry.StateOwner.State).Name);
    }
}

