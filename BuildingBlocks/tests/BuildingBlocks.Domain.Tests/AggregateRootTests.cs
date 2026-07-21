using BuildingBlocks.Domain;

namespace BuildingBlocks.Domain.Tests;

public class AggregateRootTests
{
    private static IEventSourcedAggregateRoot<TestId> AsEventSourced(TestAggregate aggregate) => aggregate;

    [Fact]
    public void Create_SetsIdentityFromFirstEvent()
    {
        var id = new TestId(Guid.NewGuid());

        var aggregate = TestAggregate.Create(id, "Soup");

        Assert.Equal(id, aggregate.Id);
        Assert.Equal("Soup", aggregate.State.Name);
    }

    [Fact]
    public void RaiseEvent_AdvancesVersionAndRecordsEvent()
    {
        var id = new TestId(Guid.NewGuid());
        var aggregate = TestAggregate.Create(id, "Soup");

        aggregate.Rename("Stew");

        Assert.Equal("Stew", aggregate.State.Name);
        Assert.Equal(2, AsEventSourced(aggregate).Version);
        Assert.Equal(2, aggregate.DomainEvents.Count);
    }

    [Fact]
    public void RaisingEventThatLeavesEmptyIdentity_Throws()
    {
        var aggregate = new TestAggregate();

        // TestRenamed does not set Id, so the identity stays empty -> guard trips.
        Assert.Throws<DomainValidationException>(() => aggregate.RaiseUnchecked(new TestRenamed("x")));
    }

    [Fact]
    public void LoadFromHistory_RebuildsStateAndVersion_ViaEsCast()
    {
        var id = new TestId(Guid.NewGuid());
        var history = new IDomainEvent[]
        {
            new TestCreated(id, "Soup"),
            new TestRenamed("Stew")
        };

        var aggregate = new TestAggregate();
        AsEventSourced(aggregate).LoadFromHistory(history);

        Assert.Equal(id, aggregate.Id);
        Assert.Equal("Stew", aggregate.State.Name);
        Assert.Equal(2, AsEventSourced(aggregate).Version);
        // Rehydration records no uncommitted events.
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void LoadFromHistory_WithEmptyIdentity_Throws()
    {
        var history = new IDomainEvent[] { new TestRenamed("x") };
        var aggregate = new TestAggregate();

        Assert.Throws<DomainValidationException>(() => AsEventSourced(aggregate).LoadFromHistory(history));
    }

    [Fact]
    public void LoadFromHistory_OnAlreadyMaterializedAggregate_Throws()
    {
        var id = new TestId(Guid.NewGuid());
        var aggregate = TestAggregate.Create(id, "Soup"); // version is now > 0

        var history = new IDomainEvent[] { new TestCreated(id, "Other") };

        Assert.Throws<DomainValidationException>(() => AsEventSourced(aggregate).LoadFromHistory(history));
    }

    [Fact]
    public void DomainEvents_AreReadOnly_AndClearRequiresManagerCast()
    {
        var id = new TestId(Guid.NewGuid());
        var aggregate = TestAggregate.Create(id, "Soup");

        Assert.Single(aggregate.DomainEvents);

        ((IDomainEventsManager)aggregate).ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }
}
